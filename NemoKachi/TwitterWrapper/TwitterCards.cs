using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Xml.Linq;

namespace NemoKachi.TwitterWrapper
{
    public class TwitterCards
    {
        public interface ITwitterCard
        {
            String Card { get; }
            String Url { get; set; }
            String Title { get; set; }
            String Description { get; set; }
            String Image { get; set; }

            String Site { get; set; }
            String SiteId { get; set; }

            String Creator { get; set; }
            String CreatorId { get; set; }
            //TwitterCardType CardType { get; }

            //SummaryCard GetSummaryCard();

            //PhotoCard GetPhotoCard();

            //PlayerCard GetPlayerCard();
        }
        public enum TwitterCardType
        {
            SummaryCard, PhotoCard, PlayerCard
        }

        public class TwitterCard : ITwitterCard
        {
            #region variations
            public String Card
            {
                get { return SpecificCard.Card; }
            }
            public String Url
            {
                get { return SpecificCard.Url; }
                set { SpecificCard.Url = value; }
            }
            public String Title
            {
                get { return SpecificCard.Title; }
                set { SpecificCard.Title = value; }
            }
            public String Description
            {
                get { return SpecificCard.Description; }
                set { SpecificCard.Description = value; }
            }
            public String Image
            {
                get { return SpecificCard.Image; }
                set { SpecificCard.Image = value; }
            }

            public String Site
            {
                get { return SpecificCard.Site; }
                set { SpecificCard.Site = value; }
            }
            public String SiteId
            {
                get { return SpecificCard.SiteId; }
                set { SpecificCard.SiteId = value; }
            }

            public String Creator
            {
                get { return SpecificCard.Creator; }
                set { SpecificCard.Creator = value; }
            }
            public String CreatorId
            {
                get { return SpecificCard.CreatorId; }
                set { SpecificCard.CreatorId = value; }
            }
            #endregion

            ITwitterCard SpecificCard { get; set; }

            public static async Task<TwitterCard> DownloadCardAsync(Uri targetUri)
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage response = await client.GetAsync(targetUri))
                    {
                        String str = await response.Content.ReadAsStringAsync();
                        //entire html string

                        {
                            Int32 startIndex = str.IndexOf("<head", StringComparison.OrdinalIgnoreCase);
                            Int32 endIndex = str.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
                            str = str.Substring(startIndex, endIndex + 7 - startIndex);
                        }

                        XElement[] metas = ParseMetatag(str);
                        //List<XElement> metas = new List<XElement>();
                        //if (str.Length > 6)
                        //{
                        //    Int32 startIndex = str.IndexOf("<meta", StringComparison.OrdinalIgnoreCase);
                        //    while (startIndex != -1)
                        //    {
                        //        Int32 endIndex = str.IndexOf('>', startIndex + 5);
                        //        XElement metaxe = null;
                        //        String metastr = "";
                        //        while (metaxe == null && endIndex > 0)
                        //        {
                        //            metastr += str.Substring(startIndex, endIndex + 1 - startIndex);
                        //            //if (metastr.LastIndexOf('<') != 0)
                        //            //    break;
                        //            try
                        //            {
                        //                if (metastr.EndsWith("/>"))
                        //                    metaxe = XElement.Parse(metastr);
                        //                else
                        //                    metaxe = XElement.Parse(metastr.Remove(metastr.Length - 1) + "/>");
                        //                metas.Add(metaxe);
                        //            }
                        //            catch (System.Xml.XmlException e)
                        //            {
                        //                endIndex = str.IndexOf(">", endIndex + 1);
                        //            }
                        //        }
                        //        if (endIndex > 0)
                        //            str = str.Substring(endIndex + 1);
                        //        else
                        //            str = str.Substring(startIndex + 5);

                        //        startIndex = str.IndexOf("<meta", StringComparison.OrdinalIgnoreCase);
                        //    }
                        //}
                        //else
                        //    return null;

                        var metaelements = new SortedDictionary<String, String>();
                        foreach (XElement metaxe in metas)
                        {
                            String name = ExtractTagName(metaxe);
                            if (name != null && (name.StartsWith("twitter:") || name.StartsWith("og:")) && !metaelements.ContainsKey(name))
                            {
                                metaelements.Add(name, ExtractTagContent(metaxe));
                            }
                        }

                        TwitterCard card = new TwitterCard();
                        var twCard = FindValue(metaelements, "twitter:card");
                        switch (twCard)
                        {
                            case null:
                            case "summary":
                                {
                                    card.SpecificCard = new SummaryCard();
                                    break;
                                }
                            case "photo":
                                {
                                    card.SpecificCard = new PhotoCard();
                                    break;
                                }
                            case "player":
                                {
                                    card.SpecificCard = new PlayerCard();
                                    break;
                                }
                            default:
                                return null;
                        }

                        card.Url = FindValue(metaelements, "twitter:url", "og:url");
                        {
                            String title = FindValue(metaelements, "twitter:title", "og:title");
                            if (title != null)
                                card.Title = WrapText(title, 70);
                        }
                        {
                            String description = FindValue(metaelements, "twitter:description", "og:description");
                            if (description != null)
                            {
                                card.Description = WrapText(description, 200);
                            }
                        }
                        card.Image = FindValue(metaelements, "twitter:image", "og:image");

                        card.Site = FindValue(metaelements, "twitter:site");
                        if (card.Site == null)
                            card.SiteId = FindValue(metaelements, "twitter:site:id");
                        card.Creator = FindValue(metaelements, "twitter:creator");
                        if (card.Creator == null)
                            card.CreatorId = FindValue(metaelements, "twitter:creator:id");

                        switch (card.Card)
                        {
                            case "summary":
                                {
                                    if (card.Url == null || card.Title == null || card.Description == null)
                                        return null;
                                    return card;
                                }
                            case "photo":
                                {
                                    if (card.Title == null || card.Image == null)
                                        return null;

                                    PhotoCard photo = card.SpecificCard as PhotoCard;
                                    photo.ImageWidth = FindValue(metaelements, "twitter:image:width", "og:image:width");
                                    if (photo.ImageWidth != null)
                                        photo.ImageHeight = FindValue(metaelements, "twitter:image:height", "og:image:height");

                                    return card;
                                }
                            case "player":
                                {
                                    if (card.Url == null || card.Title == null || card.Description == null || card.Image == null)
                                        return null;

                                    PlayerCard player = card.SpecificCard as PlayerCard;

                                    player.Player = FindValue(metaelements, "twitter:player");
                                    player.PlayerWidth = FindValue(metaelements, "twitter:player:width");
                                    player.PlayerHeight = FindValue(metaelements, "twitter:player:height");
                                    if (player.Player == null || player.PlayerWidth == null || player.PlayerWidth == null)
                                        return null;
                                    player.PlayerStream = FindValue(metaelements, "twitter:player:stream");
                                    if (player.PlayerStream != null)
                                    {
                                        player.PlayerStreamContentType = FindValue(metaelements, "twitter:player:stream:content_type");
                                        if (player.PlayerStreamContentType == null)
                                            return null;
                                    }

                                    return card;
                                }
                            default:
                                return null;
                        }
                    }
                }
            }

            static XElement[] ParseMetatag(String entirestr)
            {
                List<XElement> list = new List<XElement>();
                Int32 position = 0;
                while (true)
                {
                    position = entirestr.IndexOf("<meta", position, StringComparison.OrdinalIgnoreCase);
                    if (position != -1)
                    {
                        position += 5;
                        if (CharCompare(entirestr[position], (Char)0x09, (Char)0x0A, (Char)0x0C, (Char)0x0D, (Char)0x20, (Char)0x2F))
                        {
                            XElement xe = new XElement("meta");
                            while (true)
                            {
                                XAttribute attr = GetAnAttribute(entirestr, ref position);
                                if (attr == null)
                                {
                                    list.Add(xe);
                                    break;
                                }
                                else if (xe.Attribute(attr.Name) != null)
                                    continue;
                                xe.Add(attr);
                            }
                        }
                    }
                    else
                        break;
                }
                return list.ToArray();
            }

            static XAttribute GetAnAttribute(String entirestr, ref Int32 position)
            {
                while (true)
                {
                    if (CharCompare(entirestr[position], (Char)0x09, (Char)0x0A, (Char)0x0C, (Char)0x0D, (Char)0x20, (Char)0x2F))
                        position++;
                    else
                        break;
                }
                if (entirestr[position] == '>')
                    return null;
                else
                {
                    String namestr = "";
                    String valuestr = "";

                    //attribute name parsing
                    while (true)
                    {
                        if (entirestr[position] == '=')
                        {
                            position++;
                            goto valueparse;
                        }
                        else if (CharCompare(entirestr[position], (Char)0x09, (Char)0x0A, (Char)0x0C, (Char)0x0D, (Char)0x20))
                            goto spaceparse;
                        else if (CharCompare(entirestr[position], '/', '>'))
                            goto parsefinish;
                        else if (entirestr[position] >= 'A' && entirestr[position] <= 'Z')
                            namestr += (entirestr[position] + 0x20);
                        else
                            namestr += entirestr[position];
                        position++;
                    }

                spaceparse:
                    while (true)
                    {
                        if (CharCompare(entirestr[position], (Char)0x09, (Char)0x0A, (Char)0x0C, (Char)0x0D, (Char)0x20))
                            position++;
                        else
                            break;
                    }
                    if (entirestr[position] != '=')
                        goto parsefinish;
                    else
                        while (entirestr[position] != '=')
                            position++;
                valueparse:
                    while (true)
                    {
                        if (CharCompare(entirestr[position], (Char)0x09, (Char)0x0A, (Char)0x0C, (Char)0x0D, (Char)0x20))
                            position++;
                        else
                            break;
                    }
                    if (CharCompare(entirestr[position], '\'', '\"'))
                    {
                        Char b = entirestr[position];
                        while (true)
                        {
                            position++;
                            if (entirestr[position] == b)
                            {
                                position++;
                                goto parsefinish;
                            }
                            //else if (entirestr[position] >= 'A' && entirestr[position] <= 'Z')
                            //    valuestr += (char)(entirestr[position] + 0x20);
                            else
                                valuestr += entirestr[position];
                        }
                    }
                    else if (entirestr[position] == '>')
                        goto parsefinish;
                    //else if (entirestr[position] >= 'A' && entirestr[position] <= 'Z')
                    //{
                    //    valuestr += (entirestr[position] + 0x20);
                    //    position++;
                    //}
                    else
                    {
                        valuestr += entirestr[position];
                        position++;
                    }

                    while (true)
                    {
                        if (CharCompare(entirestr[position], (Char)0x09, (Char)0x0A, (Char)0x0C, (Char)0x0D, (Char)0x20))
                            goto parsefinish;
                        //else if (entirestr[position] >= 'A' && entirestr[position] <= 'Z')
                        //    valuestr += (entirestr[position] + 0x20);
                        else
                            valuestr += entirestr[position];
                        position++;
                    }
                parsefinish:
                    if (namestr == "")
                        return null;
                    else
                    {
                        try
                        {
                            return new XAttribute(namestr, valuestr);
                        }
                        catch
                        {
                            return null;
                        }
                    }
                }
            }

            static Boolean CharCompare(Char a, params Char[] b)
            {
                foreach (Char comp in b)
                {
                    if (a == comp)
                        return true;
                }
                return false;
            }

            static String WrapText(String target, Int32 maxLength)
            {
                if (target.Length <= maxLength)
                    return target;
                else
                    return target.Remove(maxLength - 3) + "...";
            }

            static String FindValue(SortedDictionary<String, String> dict, params String[] possibleNames)
            {
                String str = null;
                foreach (String name in possibleNames)
                {
                    dict.TryGetValue(name, out str);
                    if (str != null)
                        break;
                }
                return str;
            }

            //static XElement XAttributesToLower(XElement xe)
            //{
            //    XElement newxe = new XElement(xe.Name.ToString());
            //    foreach (XAttribute attr in xe.Attributes())
            //    {
            //        newxe.Add(new XAttribute(attr.Name.ToString().ToLower(), attr.Value));
            //    }
            //    return newxe;
            //}

            static String ExtractTagName(XElement xe)
            {
                var attr = xe.Attribute("name");
                if (attr == null)
                    attr = xe.Attribute("property");

                if (attr != null)
                    return attr.Value;
                else return null;
            }

            static String ExtractTagContent(XElement xe)
            {
                var attr = xe.Attribute("content");
                if (attr == null)
                    attr = xe.Attribute("value");

                if (attr != null)
                    return attr.Value;
                else return null;
            }
        }

        public class SummaryCard : ITwitterCard
        {
            public String Card
            {
                get { return "summary"; }
            }
            public String Url { get; set; }
            public String Title { get; set; }
            public String Description { get; set; }
            public String Image { get; set; }

            public String Site { get; set; }
            public String SiteId { get; set; }

            public String Creator { get; set; }
            public String CreatorId { get; set; }
        }

        public class PhotoCard : ITwitterCard
        {
            public String Card
            {
                get { return "photo"; }
            }
            public String Url { get; set; }
            /*
             * Photo cards are the only type of card which support a blank title.
             * To render no title for the photo card, explicitly include a blank title element.
             * For example: <meta name="twitter:title" content="">.
             * https://dev.twitter.com/docs/cards
             */
            public Boolean IsTitleExist
            {
                get { return (Title != null); }
            }
            public String Title { get; set; }
            public String Description { get; set; }
            public String Image { get; set; }

            //Provided only for Photo cards
            public String ImageWidth { get; set; }
            public String ImageHeight { get; set; }

            public String Site { get; set; }
            public String SiteId { get; set; }

            public String Creator { get; set; }
            public String CreatorId { get; set; }

            //public 
        }

        public class PlayerCard : ITwitterCard
        {
            public String Card
            {
                get { return "player"; }
            }
            public String Url { get; set; }
            public String Title { get; set; }
            public String Description { get; set; }
            public String Image { get; set; }

            public String Site { get; set; }
            public String SiteId { get; set; }

            public String Creator { get; set; }
            public String CreatorId { get; set; }

            public String Player { get; set; }
            public String PlayerWidth { get; set; }
            public String PlayerHeight { get; set; }
            public String PlayerStream { get; set; }
            public String PlayerStreamContentType { get; set; }
        }
    }
}
