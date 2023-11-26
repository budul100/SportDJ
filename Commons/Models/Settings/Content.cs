using System.Xml.Serialization;

namespace SportDJ.Commons.Models.Settings
{
    [XmlInclude(typeof(Group))]
    [XmlInclude(typeof(Player))]
    public abstract class Content
    {
        #region Public Properties

        public string Name { get; set; }

        #endregion Public Properties
    }
}