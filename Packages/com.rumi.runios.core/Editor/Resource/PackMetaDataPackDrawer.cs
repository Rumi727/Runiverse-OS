#nullable enable
using Newtonsoft.Json;
using RuniOS.Editor.Inspectors;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Csharp;
using RuniOS.IO;
using RuniOS.Resource;
using System.IO;

namespace RuniOS.Editor.Resource
{
    public sealed class PackMetaDataPackDrawer : PackDrawer
    {
        public override string targetTypeName => typeof(PackMetaData).GetTypeDisplayName();
        public override int order => int.MinValue;

        public override bool needsApplyRevert => true;

        public override bool IsMatch(IEnumerable<RuniPath> relativePaths) => relativePaths.All(x => x.IsEmpty() || x == ResourcePack.infoPath);

        protected internal override void OnEnable(PhysicalPath rootPath, IReadOnlyList<RuniPath> relativePaths)
        {
            relativeExistsPaths =
            [
                ..relativePaths
                    .Where(x => x.GetFileName() == ResourcePack.infoPath)
                    .Select<RuniPath, string>(x =>
                    {
                        if (x.IsEmpty())
                            return rootPath / ResourcePack.infoPath;

                        return rootPath / x;
                    })
                    .Where(File.Exists)
            ];

            DiscardChanges();
        }

        string[] relativeExistsPaths = [];
        PackMetaData[] packMetaDatas = [];
        static readonly InspectableObject inspectableObject = new InspectableObject(typeof(PackMetaData));
        static readonly Inspector inspector = new Inspector(UndoHandler.instance);
        protected internal override void OnGUI(PhysicalPath rootPath, IReadOnlyList<RuniPath> relativePaths, bool isDebug = false)
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
                    File.WriteAllText(rootPath / ResourcePack.infoPath, json);
                    
                    OnEnable(rootPath, relativePaths);
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