﻿using FirmwareInstaller.Models;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace FirmwareInstaller.Services.Update
{
    internal class DownloadService : BaseService
    {
        #region Constructor
        public DownloadService()
        {
            _indexType = typeof(VersionIndexModel);
            _indexExtraTypes = new Type[] { typeof(VersionModel) };
            _downloadPath = Path.GetTempPath();
        }
        #endregion

        #region Events
        #endregion

        #region Consts
        private const string _indexUrl = "https://raw.githubusercontent.com/rubenhenares/maxmix-embedded/master/versions.xml";
        #endregion

        #region Read Only
        private readonly Type _indexType;
        private readonly Type[] _indexExtraTypes;
        private readonly string _downloadPath;
        #endregion

        #region Fields
        private VersionIndexModel _versionIndexModel;
        #endregion

        #region Properties
        #endregion

        #region Private Methods
        #endregion

        #region Public Methods
        public void InitIndexFile(string filename)
        {
            XmlSerializer serializer = new XmlSerializer(_indexType, _indexExtraTypes);
            var version = new VersionModel("0.0.1.0", "http://www.sample.com/file.zip");

            var collection = new VersionIndexModel();
            collection.Versions.Add(version);

            using (TextWriter writer = new StreamWriter(filename))
                serializer.Serialize(writer, collection);
        }

        public IEnumerable<Version> RetrieveVersions()
        {
            IEnumerable<Version> result = new List<Version>();
            var serializer = new XmlSerializer(_indexType, _indexExtraTypes);
            var reader = new XmlTextReader(_indexUrl);
            try
            {
                _versionIndexModel = (VersionIndexModel)serializer.Deserialize(reader);
                result = _versionIndexModel.Versions.Select(o => o.Version);

            }
            catch
            {
                RaiseError($"Error reading version index {_indexUrl}");
            }

            return result;
        }

        public Task<string> DownloadVersionAsync(Version version)
        {

            return Task.Run<string>(() =>
            {
                var versionModel = _versionIndexModel.Versions.FirstOrDefault(o => o.Version == version);
                if (versionModel == null)
                {
                    RaiseError($"Version {version} does not exist.");
                    return string.Empty;
                }

                var versionFileName = versionModel.Url.Split('/').LastOrDefault();
                if (string.IsNullOrEmpty(versionFileName))
                {
                    RaiseError($"Error parsing url {versionModel.Url}.");
                    return string.Empty;
                }

                var downloadFilePath = Path.Combine(_downloadPath, versionFileName);
                var webClient = new WebClient();

                try
                {
                    webClient.DownloadFile(versionModel.Url, downloadFilePath);
                }
                catch
                {
                    RaiseError($"Error downloading file {versionModel.Url} to {downloadFilePath}");
                    return string.Empty;
                }

                return downloadFilePath;
            });
        }
        #endregion

        #region Event Methods
        #endregion
    }
}
