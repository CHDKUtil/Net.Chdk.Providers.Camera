using Net.Chdk.Meta.Model.Camera.Ps;
using Net.Chdk.Model.Camera;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Net.Chdk.Providers.CameraModel
{
    public abstract class PsCameraModelProvider : ProductCameraModelProvider<PsCameraData, PsCameraModelData, uint>
    {
        protected PsCameraModelProvider(string productName)
            : base(productName)
        {
        }

        protected override string GetRevision(CameraInfo cameraInfo, PsCameraModelData model)
        {
            var revisionStr = $"0x{cameraInfo.Canon.FirmwareRevision:x}";
            RevisionData revision;
            return model.Revisions.TryGetValue(revisionStr, out revision)
                ? revision.Revision
                : null;
        }

        protected override bool IsInvalid(CameraInfo cameraInfo)
        {
            return cameraInfo.Canon?.ModelId == null || cameraInfo.Canon?.FirmwareRevision == 0;
        }

        protected override CanonInfo CreateCanonInfo(ReverseCameraData camera, uint revision)
        {
            return new CanonInfo
            {
                ModelId = camera.ModelId,
                FirmwareRevision = revision
            };
        }

        protected override Dictionary<string, uint> GetVersions(PsCameraModelData model)
        {
            return model.Revisions.ToDictionary(
                kvp => kvp.Value.Revision,
                kvp => Convert.ToUInt32(kvp.Key, 16));
        }
    }
}
