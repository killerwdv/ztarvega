using System.Runtime.Serialization;

namespace ZtarVega.Models
{
    /// <summary>The web address template the user chose to download one kind of image from.</summary>
    [DataContract]
    public sealed class ImageSourceSetting
    {
        /// <summary>The image type the address is for (see <c>ImageTarget.Key</c>), for example "boxart" or "screenshots1".</summary>
        [DataMember(Name = "target")]
        public string TargetKey { get; set; }

        [DataMember(Name = "urlTemplate")]
        public string UrlTemplate { get; set; }
    }
}