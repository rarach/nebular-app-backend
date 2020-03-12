using System.Collections.Generic;


namespace NebularApi.Models.Horizon
{
    /// <summary>
    /// As seen on {HORIZON}/assets
    /// </summary>
    public class Assets
    {
        public EmbeddedAssetRecords _embedded { get; set; }
    }

    public class EmbeddedAssetRecords
    {
        public List<AssetData> records { get; set; }
    }

    public class AssetData
    {
        public Links _links { get; set; }
    }

    public class Links
    {
        public TomlLink toml { get; set; }
    }

    public class TomlLink
    {
        public string href { get; set; }
    }
}
