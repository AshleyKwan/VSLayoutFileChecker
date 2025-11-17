#nullable disable

//using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using VSLayoutFile;
using static System.Formats.Asn1.AsnWriter;

namespace VSLayoutFile
{

    public class ChannelManifestInfo
    {
        public string id { get; set; }
        public Version buildVersion { get; set; }
        public string productDisplayVersion { get; set; }

        public string productName { get; set; }
        public string productLineVersion { get; set; }
        public string productRelease { get; set; }
    }
    public class ChannelManifest
    {

        public Version manifestVersion { get; set; }

        public Version engineVersion { get; set; }
        public ChannelManifestInfo info { get; set; }

        public List<ChannelPackage> channelItems { get; set; }

        public List<ChannelPackage> packages { get; set; }

        public List<ChannelPackage> FindPackageByType(string typeValue)
        {
            List<ChannelPackage> result = new List<ChannelPackage>();
            foreach (var item in this.packages)
            {
                if (item.type.ToLower() == typeValue.ToLower())
                    result.Add(item);
            }
            return result;
        }

        public List<ChannelPackage> FindPackageById(string Id)
        {
            List<ChannelPackage> result = new List<ChannelPackage>();
            foreach (var item in this.packages)
            {
                if (item.id.ToLower() == Id.ToLower())
                    result.Add(item);
            }
            return result;
        }

        public List<ChannelPackage> FindChannelItems(string typeValue)
        {
            List<ChannelPackage> result = new List<ChannelPackage>();
            foreach (var item in this.channelItems)
            {
                if (item.type.ToLower() == typeValue.ToLower())
                    result.Add(item);
            }
            return result;
        }

        public List<ChannelPackage> FindByCPUArc(System.Runtime.InteropServices.Architecture cpuArc)
        {
            List<ChannelPackage> result = new List<ChannelPackage>();
            foreach (var item in this.packages)
            {
                if (item.productArch.ToLower() == cpuArc.ToString().ToLower())
                    result.Add(item);
            }
            return result;
        }

        public List<String> FindPackageCatelog()
        {
            List<String> result = new List<String>();
            foreach (var item in this.packages)
            {
                if (!result.Contains(item.type))
                    result.Add(item.type);


            }
            return result;
        }

        public void ConstructDependencies()
        {
            foreach (var package in this.packages)
            {
                if (package.dependencies != null)
                {
                    foreach (var dependency in package.dependencies)
                    {
                        List<ChannelPackage> packages = FindPackageById(dependency.Key);
                        if (packages.Count > 0)
                            package.Dependency.Add(packages[0]);
                        //  string dependencyID = dependency.Key;
                        //  DependencyVersion dependencyVersion = new DependencyVersion(dependency.Value.ToString());

                        //  if (((JsonElement)dependency.Value).ValueKind == JsonValueKind.Object)
                        //  {
                        //      dependencyVersion = ((JsonElement)dependency.Value).Deserialize<DependencyVersion>();
                        //  }

                        ////  string version = dependency.Value.ToString();
                        //  Dependency dep = new Dependency(dependencyID, dependencyVersion);
                        //  package.Dependencies.Add(dep);
                    }
                }
            }
        }
    }

    public class ChannelPackage
    {
        [DataMember(Order = 0)]
        public string id { get; set; }
        public string version { get; set; }
        public List<ChannelPayload> payloads { get; set; }
        public string chip { get; set; }

        public string productArch { get; set; }
        public string machineArch { get; set; }

        public string language { get; set; }
        public string type { get; set; }

        public Dictionary<string, object> dependencies { get; set; }
        public List<ChannelPackage> Dependency = new List<ChannelPackage>();
        //public DependencyCollection Dependencies=new DependencyCollection();

        public Executable layoutParams { get; set; }

        public Executable layoutInstallParams { get; set; }

        public string productCode { get; set; }
        public string upgradeCode { get; set; }
        public string productVersion { get; set; }
        public int productLanguage { get; set; }
        public string providerKey { get; set; }
        public InstallSizes installSizes { get; set; }
        public LogFile[] logFile { get; set; }
        //   public JsonNode dependencies { get; set; }
    }

    public class InstallSizes
    {
        public long systemDrive { get; set; }
    }

    public class LogFile
    {
        public string pattern { get; set; }
    }

    public class ChannelPayload
    {
        public string fileName { get; set; }
        public string sha256 { get; set; }
        public long size { get; set; }
        public string url { get; set; }
        public bool isDynamicEndpoint { get; set; }

    }

    public class Executable
    {
        public string fileName { get; set; }
        public string parameters { get; set; }
    }


    public class DependencyCollection : IEnumerable<Dependency>, ICollection<Dependency>, IEnumerable<KeyValuePair<string, Dependency>>, IEnumerable
    {
        private IDictionary<string, Dependency> dependency = new Dictionary<string, Dependency>(StringComparer.OrdinalIgnoreCase);


        public int Count => dependency.Count;

        public bool IsReadOnly => false;// throw new NotImplementedException();

        private static string GetKeyForItem(Dependency item)
        {
            return GetKey(item.Id, item.When);
        }

        private static string GetKey(string dependencyId, ICollection<string> when)
        {
            return dependencyId;
        }

        public void Add(Dependency item)
        {
            

          //  string keyForItem = GetKeyForItem(item);
            dependency.Add(item.Id, item);
            //   dependency.Add(item);
        }

        public void Clear()
        {
            dependency.Clear();
        }

        public bool Contains(Dependency item)
        {
            string keyForItem = GetKeyForItem(item);
            return dependency.ContainsKey(keyForItem);

            //throw new NotImplementedException();
            //  return dependency.Contains(item);
        }

        void ICollection<Dependency>.CopyTo(Dependency[] array, int arrayIndex)
        {
            dependency.Values.CopyTo(array, arrayIndex);
        }


        public IEnumerator<Dependency> GetEnumerator()
        {

            return dependency.Values.GetEnumerator();
        }

        public bool Remove(Dependency item)
        {
            string getKeyForItem = GetKeyForItem(item);
            return dependency.Remove(getKeyForItem);

        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return dependency.Values.GetEnumerator();
        }

        IEnumerator<KeyValuePair<string, Dependency>> IEnumerable<KeyValuePair<string, Dependency>>.GetEnumerator()
        {
            return dependency.GetEnumerator();
        }
    }

    public class Dependency// : ChannelPackage
    {
     

        public Dependency(string dependencyID,string version)
        {
            this.Id= dependencyID;
            this.version = new DependencyVersion( version);
        }

        public Dependency(string key, DependencyVersion value)
        {
            this.Id = key;
            this.version = value;
        }


        public string Id = "";
        public DependencyVersion version;
        
        public ISet<string> When { get; set; }

    }

    public class DependencyVersion
    {
        public DependencyVersion() { }

        public DependencyVersion(string version)
        {
            this.Version = version;
        }

        public DependencyVersion(DependencyVersion version)
        {
            this.Version = version.Version;
            this.Type= version.Type;
            this.Behaviors= version.Behaviors;  
        }

        public string Version { get; set; }
        public string Type { get; set; }
        public string Behaviors { get; set; }
    }

    public class LocalizedResources
    {
        public string language { get; set; }
        public string Description { get; set; }
        public string title { get; set; }
        public string category { get; set; }

    }





    public interface IPackage : IPackageIdentity, IEquatable<IPackageIdentity>//, ILocalizedResources
    {
        //  PackageType Type { get; }

        Uri License { get; }

        DependencyCollection Dependencies { get; }

        //BreadcrumbTemplate BreadcrumbTemplate { get; }

        //CurrentState CurrentState { get; set; }

        //RequestedState RequestedState { get; set; }

        //DetectedState DetectedState { get; set; }

        IList<string> TelemetryCorrelatedParents { get; }

        IList<string> AncestorWorkloads { get; }

        //IList<ProjectClassifier> ProjectClassifiers { get; }

        //ApplicabilityState ApplicabilityState { get; set; }

        bool HasVitalFailure { get; set; }

        bool OutOfSupport { get; set; }

        ISet<IPackage> SelectedParents { get; }

        ISet<IPackage> SelectedRequiredChildren { get; }

        // VisualStudioInformation VisualStudioInformation { get; set; }

        IPackage SupersedingPackage { get; set; }

        bool Replace { get; set; }

        IPackage DowngradingPackage { get; set; }

        IPackage PackageToDowngrade { get; set; }

        //   OriginInfo Origin { get; set; }
    }

    public interface IPackageIdentity : IEquatable<IPackageIdentity>
    {
        string Id { get; }

        Version Version { get; }

        string Chip { get; }

        string Language { get; }

        string Branch { get; }

        string ProductArch { get; }

        string MachineArch { get; }

        string GetUniqueId();
    }

    class Service : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }
    }


}

