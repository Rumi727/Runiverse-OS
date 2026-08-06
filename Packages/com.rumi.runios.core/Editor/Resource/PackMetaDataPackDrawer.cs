#nullable enable
using Newtonsoft.Json;
using RuniOS.Editor.Inspectors;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Csharp;
using RuniOS.IO;
using RuniOS.Resource;
using System.Collections.Immutable;
using System.IO;

namespace RuniOS.Editor.Resource
{
    public sealed class PackMetaDataPackDrawer(ImmutableArray<PackDrawer.PathPair> targets) : PackDrawer(targets)
    {
        public override string targetTypeName => typeof(PackMetaData).GetTypeDisplayName();
        public override int order => int.MinValue;

        public override bool needsApplyRevert => true;

        public override bool IsMatch(IEnumerable<RuniPath> relativePaths) => relativePaths.All(x => x.IsEmpty() || x == ResourcePack.infoPath);

        string[] relativeExistsPaths = [];
        PackMetaData[] packMetaDatas = [];

        protected internal override void OnEnable()
        {
            relativeExistsPaths =
            [
                ..targets
                    .Where(x => x.relativePath.GetFileName() == ResourcePack.infoPath)
                    .Select<PathPair, string>(x =>
                    {
                        if (x.relativePath.IsEmpty())
                            return x.rootPath / ResourcePack.infoPath;

                        return x.rootPath / x.relativePath;
                    })
                    .Where(File.Exists)
            ];

            DiscardChanges();
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

            if (packMetaDatas.Length == 0)
            {
                if (GUILayout.Button(GetTextOrKey("pack_drawer.pack_meta_data.create")))
                {
                    string json = JsonConvert.SerializeObject(new PackMetaData(), Formatting.Indented);
                    foreach (var target in targets)
                        File.WriteAllText(target.rootPath / ResourcePack.infoPath, json);

                    OnEnable();
                }
                
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

        public override void SaveChanges()
        {
            foreach ((string path, PackMetaData instance) in relativeExistsPaths.Zip(packMetaDatas, (path, instance) => (path, instance)))
            {
                string json = JsonConvert.SerializeObject(instance, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            
            base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            packMetaDatas = relativeExistsPaths
                .Select(File.ReadAllText)
                .Select(JsonConvert.DeserializeObject<PackMetaData>)
                .ToArray();
            
            base.DiscardChanges();
        }
    }
}