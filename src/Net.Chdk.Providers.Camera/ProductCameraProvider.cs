using Microsoft.Extensions.Logging;
using Net.Chdk.Meta.Model.Camera;
using Net.Chdk.Model.Camera;
using Net.Chdk.Model.CameraModel;
using Net.Chdk.Model.Software;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Net.Chdk.Providers.Camera
{
    abstract class ProductCameraProvider<TCamera, TModel, TCard, TVersion> : DataProvider<Dictionary<string, TCamera>>, IProductCameraProvider
        where TCamera : CameraData<TCamera, TModel, TCard>
        where TModel : CameraModelData
        where TCard : CardData
    {
        protected sealed class ReverseCameraData
        {
            public string[] Models { get; set; }
            public Dictionary<string, TVersion> Versions { get; set; }
            public uint ModelId { get; set; }
            public SoftwareEncodingInfo Encoding { get; set; }
        }

        private const string DataFileName = "cameras.json";

        private string ProductName { get; }

        protected ProductCameraProvider(string productName, ILogger logger)
            : base(logger)
        {
            ProductName = productName;
            _reverseCameras = new Lazy<Dictionary<string, ReverseCameraData>>(GetReverseCameras);
        }

        public SoftwareCameraInfo GetCamera(string productName, CameraInfo cameraInfo, CameraModelInfo cameraModelInfo)
        {
            if (!ProductName.Equals(productName, StringComparison.Ordinal))
                return null;

            var camera = GetCamera(cameraInfo);
            if (camera == null)
                return null;

            var model = camera.Models.SingleOrDefault(m => m.Names[0].Equals(cameraModelInfo.Names[0], StringComparison.Ordinal));
            if (model == null)
                return null;

            return new SoftwareCameraInfo
            {
                Platform = model.Platform,
                Revision = GetRevision(cameraInfo, model),
            };
        }

        protected abstract string GetRevision(CameraInfo cameraInfo, TModel model);

        public SoftwareEncodingInfo GetEncoding(SoftwareProductInfo productInfo, SoftwareCameraInfo cameraInfo)
        {
            if (!ProductName.Equals(productInfo?.Name, StringComparison.Ordinal))
                return null;

            return GetCameraModel(cameraInfo, out ReverseCameraData camera)
                ? camera.Encoding
                : null;
        }

        public CameraModelsInfo GetCameraModels(CameraInfo cameraInfo)
        {
            var camera = GetCamera(cameraInfo);
            if (camera == null)
                return null;

            var models = new CameraModelInfo[camera.Models.Length];
            for (var i = 0; i < camera.Models.Length; i++)
            {
                models[i] = new CameraModelInfo
                {
                    Names = camera.Models[i].Names
                };
            }
            return GetCameraModels(camera, models);
        }

        protected virtual CameraModelsInfo GetCameraModels(TCamera camera, CameraModelInfo[] models)
        {
            return new CameraModelsInfo
            {
                Models = models,
                CardType = camera.Card?.Type,
                CardSubtype = camera.Card?.Subtype,
                BootFileSystem = camera.Boot?.Fs,
            };
        }

        public CameraModelsInfo GetCameraModels(SoftwareCameraInfo cameraInfo)
        {
            if (!GetCameraModel(cameraInfo, out ReverseCameraData camera))
                return null;

            if (!GetCamera(camera, cameraInfo, out TVersion version))
                return null;

            return GetCameraModels(camera, version);
        }

        protected virtual CameraModelsInfo GetCameraModels(ReverseCameraData camera, TVersion version)
        {
            return new CameraModelsInfo
            {
                Info = new CameraInfo
                {
                    Base = CreateBaseInfo(camera),
                    Canon = CreateCanonInfo(camera, version),
                },
                Models = new[]
                {
                    new CameraModelInfo
                    {
                        Names = camera.Models
                    }
                },
            };
        }

        protected abstract bool IsInvalid(CameraInfo cameraInfo);

        protected abstract CanonInfo CreateCanonInfo(ReverseCameraData camera, TVersion version);

        protected abstract Dictionary<string, TVersion> GetVersions(TModel model);

        private static BaseInfo CreateBaseInfo(ReverseCameraData camera)
        {
            return new BaseInfo
            {
                Make = "Canon",
                Model = string.Join("\n", camera.Models)
            };
        }

        private bool GetCamera(ReverseCameraData value, SoftwareCameraInfo camera, out TVersion version)
        {
            return value.Versions.TryGetValue(camera.Revision, out version);
        }

        private TCamera GetCamera(CameraInfo cameraInfo)
        {
            if (IsInvalid(cameraInfo))
                return null;

            var modelId = $"0x{cameraInfo.Canon.ModelId:x}";
            if (!Data.TryGetValue(modelId, out TCamera camera))
                return null;

            return camera;
        }

        private bool GetCameraModel(SoftwareCameraInfo cameraInfo, out ReverseCameraData camera)
        {
            camera = null;

            if (cameraInfo == null)
                return false;

            return ReverseCameras.TryGetValue(cameraInfo.Platform, out camera);
        }

        protected override string GetFilePath()
        {
            return Path.Combine(Directories.Data, Directories.Product, ProductName, DataFileName);
        }

        #region ReverseCameras

        private readonly Lazy<Dictionary<string, ReverseCameraData>> _reverseCameras;

        private Dictionary<string, ReverseCameraData> ReverseCameras => _reverseCameras.Value;

        private Dictionary<string, ReverseCameraData> GetReverseCameras()
        {
            var reverseCameras = new Dictionary<string, ReverseCameraData>();
            foreach (var kvp in Data)
            {
                foreach (var model in kvp.Value.Models)
                {
                    var camera = CreateReverseCamera(kvp.Key, kvp.Value, model);
                    reverseCameras.Add(model.Platform, camera);
                }
            }
            return reverseCameras;
        }

        private ReverseCameraData CreateReverseCamera(string key, TCamera camera, TModel model)
        {
            return new ReverseCameraData
            {
                ModelId = Convert.ToUInt32(key, 16),
                Versions = GetVersions(model),
                Encoding = GetEncoding(camera),
                Models = model.Names,
            };
        }

        private static SoftwareEncodingInfo GetEncoding(TCamera camera)
        {
            return new SoftwareEncodingInfo
            {
                Name = camera.Encoding.Name,
                Data = camera.Encoding.Data,
            };
        }

        #endregion
    }
}
