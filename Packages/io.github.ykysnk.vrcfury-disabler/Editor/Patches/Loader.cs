using io.github.ykysnk.utils.Editor.Patches;
using io.github.ykysnk.VRCFuryDisabler.Editor.Patches;

[assembly: ExportsPatchLoader(typeof(Loader))]

namespace io.github.ykysnk.VRCFuryDisabler.Editor.Patches
{
    public class Loader : PatchLoader<Loader>
    {
        public override string QualifiedName => "io.github.ykysnk.vrcfury-disabler.patches";
        public override string DisplayName => "VRCFury Disabler";

        public override void Load()
        {
            VRCFuryBuilderPatch.Instance.Run();
        }
    }
}