using System.Xml.Serialization;

[XmlRoot("titlepatch")]
public class TitlePatch
{
    [XmlAttribute("status")]
    public required string Status { get; set; }

    [XmlAttribute("titleid")]
    public required string TitleID { get; set; }

    [XmlElement("tag")]
    public required Tag Tag { get; set; }
}

public class Tag
{
    [XmlAttribute("name")]
    public required string Name { get; set; }

    [XmlAttribute("popup")]
    public bool Popup { get; set; }

    [XmlAttribute("signoff")]
    public bool SignOff { get; set; }

    [XmlElement("package")]
    public required List<Package> Packages { get; set; }
}

public class Package
{
    [XmlAttribute("version")]
    public required string Version { get; set; }

    [XmlAttribute("size")]
    public required int Size { get; set; }

    [XmlAttribute("sha1sum")]
    public required string Checksum { get; set; }

    [XmlAttribute("url")]
    public required string URL { get; set; }

    [XmlAttribute("ps3_system_ver")]
    public required string Firmware { get; set; }

    [XmlElement("paramsfo")]
    public required Params Params { get; set; }
}

public class Params
{
    [XmlElement("TITLE")]
    public required string Title { get; set; }

    [XmlElement("TITLE_01")]
    public string? AlternateTitle { get; set; }
}
