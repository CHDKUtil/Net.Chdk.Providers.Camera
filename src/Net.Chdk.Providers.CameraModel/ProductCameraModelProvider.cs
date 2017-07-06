using Net.Chdk.Detectors.CameraModel;
using Net.Chdk.Meta.Model.Camera;
using Net.Chdk.Model.Camera;
using Net.Chdk.Model.CameraModel;
using Net.Chdk.Model.Software;
using Net.Chdk.Providers.Software;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Net.Chdk.Providers.CameraModel
{
    public abstract class ProductCameraModelProvider<TCamera, TModel, TVersion> : IProductCameraProvider, IProductCameraModelProvider, IProductCameraModelDetector
        where TCamera : CameraData<TCamera, TModel>
        where TModel : CameraModelData
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

        protected ProductCameraModelProvider(string productName)
        {
            ProductName = productName;
            _cameras = new Lazy<Dictionary<string, TCamera>>(GetCameras);
            _reverseCameras = new Lazy<Dictionary<string, ReverseCameraData>>(GetReverseCameras);
        }

        SoftwareCameraInfo IProductCameraProvider.GetCamera(string productName, CameraInfo cameraInfo, CameraModelInfo cameraModelInfo)
        {
            if (!ProductName.Equals(productName, StringComparison.InvariantCulture))
                return null;

            var camera = GetCamera(cameraInfo);
            if (camera == null)
                return null;

            var model = camera.Models.SingleOrDefault(m => m.Names[0].Equals(cameraModelInfo.Names[0], StringComparison.InvariantCulture));
            if (model == null)
                return null;

            return new SoftwareCameraInfo
            {
                Platform = model.Platform,
                Revision = GetRevision(cameraInfo, model),
            };
        }

        protected abstract string GetRevision(CameraInfo cameraInfo, TModel model);

        SoftwareEncodingInfo IProductCameraProvider.GetEncoding(SoftwareProductInfo productInfo, SoftwareCameraInfo cameraInfo)
        {
            ReverseCameraData camera;
            return GetCameraModel(productInfo?.Name, cameraInfo, out camera)
                ? camera.Encoding
                : null;
        }

        CameraModelsInfo IProductCameraModelProvider.GetCameraModels(CameraInfo cameraInfo)
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
            return new CameraModelsInfo
            {
                Models = models,
                CardType = camera.Card?.Type,
                CardSubtype = camera.Card?.Subtype,
                BootFileSystem = camera.Boot.Fs,
            };
        }

        CameraModels IProductCameraModelDetector.GetCameraModels(SoftwareInfo softwareInfo, IProgress<double> progress, CancellationToken token)
        {
            var productInfo = softwareInfo?.Product;
            var cameraInfo = softwareInfo?.Camera;

            ReverseCameraData camera;
            if (!GetCameraModel(productInfo?.Name, cameraInfo, out camera))
                return null;

            TVersion version;
            if (!GetCamera(camera, cameraInfo, out version))
                return null;

            return new CameraModels
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
            TCamera camera;
            if (!Cameras.TryGetValue(modelId, out camera))
                return null;

            return camera;
        }

        private bool GetCameraModel(string productName, SoftwareCameraInfo cameraInfo, out ReverseCameraData camera)
        {
            camera = null;

            if (!ProductName.Equals(productName, StringComparison.InvariantCulture))
                return false;

            if (cameraInfo == null)
                return false;

            return ReverseCameras.TryGetValue(cameraInfo.Platform, out camera);
        }

        #region Cameras

        private readonly Lazy<Dictionary<string, TCamera>> _cameras;

        protected Dictionary<string, TCamera> Cameras => _cameras.Value;

        private Dictionary<string, TCamera> GetCameras()
        {
            var filePath = Path.Combine(Directories.Data, Directories.Product, ProductName, DataFileName);
            using (var reader = File.OpenText(filePath))
            using (var jsonReader = new JsonTextReader(reader))
            {
                var serializer = JsonSerializer.CreateDefault();
                return serializer.Deserialize<Dictionary<string, TCamera>>(jsonReader);
            }
        }

        #endregion

        #region ReverseCameras

        private readonly Lazy<Dictionary<string, ReverseCameraData>> _reverseCameras;

        private Dictionary<string, ReverseCameraData> ReverseCameras => _reverseCameras.Value;

        private Dictionary<string, ReverseCameraData> GetReverseCameras()
        {
            var reverseCameras = new Dictionary<string, ReverseCameraData>();
            foreach (var kvp in Cameras)
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
                Encoding = camera.Encoding,
                Models = model.Names,
            };
        }

        #endregion
    }
}
