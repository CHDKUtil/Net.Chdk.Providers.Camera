using Net.Chdk.Meta.Model.Camera.Ps;
using System.Collections.Generic;

namespace Net.Chdk.Providers.Camera
{
    sealed class PsReverseCameraData : ReverseCameraData
    {
        public Dictionary<string, uint> Revisions { get; set; }
        public AltData Alt { get; set; }
    }
}
