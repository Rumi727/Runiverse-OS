#nullable enable
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using RuniOS.Editor.Inspectors;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Csharp;
using RuniOS.IO;
using RuniOS.Resource;
using System.Collections.Immutable;

namespace RuniOS.Editor.Resource
{
    public sealed class PackMetaDataPackDrawer(ImmutableArray<IONode> targets) : PackDrawer(targets)
    {
        public override string targetTypeName => typeof(PackMetaData).GetTypeDisplayName();
        public override int order => int.MinValue;

        public override bool needsApplyRevert => true;

        public override bool IsMatch(IEnumerable<RuniPath> relativePaths) => relativePaths.Any() && relativePaths.All(x => x.IsEmpty() || x == ResourcePack.infoPath);

        // 실제로 있는 pack.json이며, 아직 불러오지 않았으면 default입니다.
        ImmutableArray<IONode> infoNodes;
        PackMetaData[] packMetaDatas = [];

        protected internal override UniTask OnEnable() => Load();

        // 위치의 루트를 대상으로 하면 그 루트의 pack.json을 편집합니다.
        static IONode GetInfoNode(IONode node) => node.path.IsEmpty() ? node.CreateChild(ResourcePack.infoPath) : node;

        async UniTask Load()
        {
            ImmutableArray<IONode>.Builder nodes = ImmutableArray.CreateBuilder<IONode>(targets.Length);
            foreach (IONode target in targets)
            {
                IONode infoNode = GetInfoNode(target);
                if (await infoNode.file.Exists())
                    nodes.Add(infoNode);
            }

            ImmutableArray<IONode> loadedNodes = nodes.ToImmutable();
            PackMetaData[] loadedMetaDatas = await Read(loadedNodes);

            infoNodes = loadedNodes;
            packMetaDatas = loadedMetaDatas;

            Repaint();
        }

        static async UniTask<PackMetaData[]> Read(ImmutableArray<IONode> nodes)
        {
            PackMetaData[] metaDatas = new PackMetaData[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
                metaDatas[i] = JsonConvert.DeserializeObject<PackMetaData?>(await nodes[i].file.ReadAllText()) ?? new PackMetaData();

            return metaDatas;
        }

        async UniTaskVoid Create()
        {
            string json = JsonConvert.SerializeObject(new PackMetaData(), Formatting.Indented);
            foreach (IONode target in targets)
            {
                // 쓸 수 없는 위치에는 만들지 않습니다.
                if (GetInfoNode(target).AsWriteNode() is { } infoNode)
                    await infoNode.file.WriteAllText(json);
            }

            await Load();
        }

        static readonly InspectableObject inspectableObject = new InspectableObject(typeof(PackMetaData));
        static readonly Inspector inspector = new Inspector(UndoHandler.instance);
        protected internal override void OnGUI(bool isDebug = false)
        {
            GUILayout.Label(TrTempContent("runios-editor:pack_drawer.generic.title"), RuniStyles.largeLabel);

            InspectorFlags flags = InspectorFlags.InstanceAccess | InspectorFlags.Variable;
            if (isDebug)
                flags |= InspectorFlags.Debug;
                
            if (inspector.inspectorFlags != flags)
                inspector.Rebuild(inspectableObject, flags);

            // 불러오는 동안에는 만들기 버튼을 보여주지 않습니다.
            if (infoNodes.IsDefault)
                return;

            if (packMetaDatas.Length == 0)
            {
                if (GUILayout.Button(GetTextOrKey("runios-editor:pack_drawer.pack_meta_data.create")))
                    Create().Forget();

                return;
            }
            
            inspectableObject.SetInstances(packMetaDatas);
            
            inspectableObject.onValueChanged = instances =>
            {
                int index = 0;
                foreach (var instance in instances.Cast<PackMetaData>())
                {
                    packMetaDatas[index] = instance;
                    index++;
                }
                
                SetDirty();
            };

            inspector.DrawLayout();
        }

        // 저장을 시작한 시점의 대상과 값을 씁니다. 쓸 수 없는 위치는 건너뜁니다.
        public override async UniTask SaveChanges()
        {
            if (!infoNodes.IsDefault)
            {
                for (int i = 0; i < Min(infoNodes.Length, packMetaDatas.Length); i++)
                {
                    if (infoNodes[i].AsWriteNode() is { } infoNode)
                        await infoNode.file.WriteAllText(JsonConvert.SerializeObject(packMetaDatas[i], Formatting.Indented));
                }
            }

            await base.SaveChanges();
        }

        public override async UniTask DiscardChanges()
        {
            ImmutableArray<IONode> nodes = infoNodes;
            if (!nodes.IsDefault)
            {
                PackMetaData[] metaDatas = await Read(nodes);

                // 읽는 동안 다시 불러왔으면 그 결과를 둡니다.
                if (nodes == infoNodes)
                {
                    packMetaDatas = metaDatas;
                    Repaint();
                }
            }

            await base.DiscardChanges();
        }
    }
}