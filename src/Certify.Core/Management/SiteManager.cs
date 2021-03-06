﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Certify.Models;

namespace Certify.Management
{
    public class SiteManager
    {
        private const string APPDATASUBFOLDER = "Certify";
        private const string SITEMANAGERCONFIG = "sites.json";
        public bool IsLocalIISMode { get; set; } //TODO: driven by config

        private List<ManagedSite> managedSites { get; set; }

        public SiteManager()
        {
            IsLocalIISMode = true;
            this.managedSites = this.Preview();
        }

        private string GetAppDataFolder()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\" + APPDATASUBFOLDER;
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }
            return path;
        }

        public void StoreSettings()
        {
            string appDataPath = GetAppDataFolder();
            string siteManagerConfig = Newtonsoft.Json.JsonConvert.SerializeObject(this.managedSites);
            System.IO.File.WriteAllText(appDataPath + "\\" + SITEMANAGERCONFIG, siteManagerConfig);
        }

        public void LoadSettings()
        {
            string appDataPath = GetAppDataFolder();
            string configData = System.IO.File.ReadAllText(appDataPath + "\\" + SITEMANAGERCONFIG);
            this.managedSites = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ManagedSite>>(configData);
        }

        /// <summary>
        /// For current configured environment, show preview of recommended site management (for local IIS, scan sites and recommend actions)
        /// </summary>
        /// <returns></returns>
        public List<ManagedSite> Preview()
        {
            List<ManagedSite> sites = new List<ManagedSite>();

            if (IsLocalIISMode)
            {
                try
                {
                    var iisSites = new IISManager().GetSiteList(includeOnlyStartedSites: true).OrderBy(s => s.SiteId).ThenBy(s => s.Host);

                    var siteIds = iisSites.GroupBy(x => x.SiteId);

                    foreach (var s in siteIds)
                    {
                        ManagedSite managedSite = new ManagedSite { SiteId = s.Key };
                        managedSite.SiteType = ManagedSiteType.LocalIIS;
                        managedSite.Server = "localhost";
                        managedSite.SiteName = iisSites.First(i => i.SiteId == s.Key).SiteName;
                        managedSite.SiteBindings = new List<ManagedSiteBinding>();

                        foreach (var binding in s)
                        {
                            var managedBinding = new ManagedSiteBinding { Hostname = binding.Host, IP = binding.IP, Port = binding.Port, UseSNI = true, CertName = "Certify_" + binding.Host };
                            managedSite.SiteBindings.Add(managedBinding);
                        }
                        sites.Add(managedSite);
                    }
                }
                catch (Exception)
                {
                    //can't read sites
                    System.Diagnostics.Debug.WriteLine("Can't get IIS site list.");
                }
            }
            return sites;
        }
    }
}