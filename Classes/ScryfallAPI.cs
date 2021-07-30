using MTG_builder;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MTG.Scryfall
{
    /// <summary>
    /// Scryfall API
    /// Docs: https://scryfall.com/docs/api
    /// </summary>
    public class ScryfallAPI
    {
        public static readonly string SetListsUrl = "https://api.scryfall.com/sets/";

        /// <summary>
        /// Returns list of cards from a card set using Scryfall API
        /// </summary>
        public static List<Card> FetchScryfallSetCards(string searchUri)
        {
            List<Card> cards = new();

            using WebClient wc = new();

            string json = wc.DownloadString(searchUri);
            CardData cardData = JsonConvert.DeserializeObject<CardData>(json);
            cards.AddRange(cardData.Data);

            while (cardData.HasMore)
            {
                json = wc.DownloadString(cardData.NextPage);
                cardData = JsonConvert.DeserializeObject<CardData>(json);
                cards.AddRange(cardData.Data);
            }

            return cards;
        }

        public static async Task DownloadCardImages(List<Card> cards)
        {
            List<Task> tasks = new();
            foreach (Card card in cards)
            {
                tasks.Add(card.DownloadCardImagesAsync());
            }

            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// MTG Card using Scryfall API
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Card
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("image_uris")]
        public Dictionary<string, string> ImageUris { get; set; }
        [JsonProperty("image_status")]
        public string ImageStatus { get; set; }
        [JsonProperty("cardmarket_id")]
        public int CardmarketId { get; set; }
        [JsonProperty("card_faces")]
        public List<CardFace> CardFaces { get; set; }
        [JsonProperty("cmc")]
        public decimal CMC { get; set; }
        [JsonProperty("color_identity")]
        public List<CardColor> ColorIdentity { get; set; }
        [JsonProperty("keywords")]
        public List<string> Keywords { get; set; }
        [JsonProperty("type_line")]
        public string TypeLine { get; set; }
        [JsonProperty("rarity")]
        public CardRarity Rarity { get; set; }
        [JsonProperty("set")]
        public string Set { get; set; }
        [JsonIgnore]
        [JsonProperty("legalities")]
        public Dictionary<string, string> Legalities { get; set; }

        [JsonIgnore]
        public BitmapImage PrimaryFace
        {
            get
            {
                string filePath = $"{IO.CardImagePath}{Id}.png";
                try
                {
                    return File.Exists(filePath) ? (new(new Uri(Path.GetFullPath(filePath)))) : (new(new Uri(PrimaryFaceURI)));
                }
                catch (Exception)
                {
                    try
                    {
                        IO.DownloadFile(filePath, PrimaryFaceURI);
                        return new(new Uri(Path.GetFullPath(filePath)));
                    }
                    catch (Exception)
                    {
                        return new(new Uri(PrimaryFaceURI));
                    }
                }
            }
        }
        [JsonIgnore]
        public BitmapImage SecondaryFace
        {
            get
            {
                string filePath = $"{IO.CardImagePath}{Id}back.png";
                try
                {
                    return File.Exists(filePath) ? (new(new Uri(Path.GetFullPath(filePath)))) : (new(new Uri(SecondaryFaceURI)));
                }
                catch (Exception)
                {
                    try
                    {
                        IO.DownloadFile(filePath, SecondaryFaceURI);
                        return new(new Uri(Path.GetFullPath(filePath)));
                    }
                    catch (Exception)
                    {
                        return new(new Uri(SecondaryFaceURI));
                    }
                }
            }
        }
        [JsonIgnore]
        public string PrimaryFaceURI
        {
            get
            {
                if (ImageUris == null)
                {
                    if (CardFaces.Count > 1 && CardFaces[0].ImageUris != null)
                    {
                        return CardFaces[0].ImageUris["normal"];
                    }

                    return null;
                }
                else
                {
                    return ImageUris["normal"];
                }
            }
        }
        [JsonIgnore]
        public string SecondaryFaceURI
        {
            get
            {
                if (!HasTwoFaces) { return null; }
                else
                {
                    return CardFaces[1].ImageUris["normal"];
                }
            }
        }
        [JsonIgnore]
        public bool HasTwoFaces => ImageUris == null && CardFaces != null;
        [JsonIgnore]
        public CardColor GetColorIdentity => ColorIdentity.Count == 0 ? CardColor.Colorless : ColorIdentity.Count > 1 ? CardColor.Multicolor : ColorIdentity[0];

        //Gameplay
        [JsonIgnore]
        public bool Tapped { get; set; }

        public async Task DownloadCardImagesAsync()
        {
            if (!File.Exists($"{IO.CardImagePath}{Id}.png"))
            {
                await IO.DownloadFileAsync($"{IO.CardImagePath}{Id}.png", PrimaryFaceURI);
            }
            if (HasTwoFaces && !File.Exists($"{IO.CardImagePath}{Id}back.png"))
            {
                await IO.DownloadFileAsync($"{IO.CardImagePath}{Id}back.png", SecondaryFaceURI);
            }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum CardColor
        {
            W, U, B, R, G, Colorless, Multicolor
        }
        [JsonConverter(typeof(StringEnumConverter))]
        public enum CardRarity
        {
            common, uncommon, rare, special, mythic, bonus
        }
    }
    /// <summary>
    /// MTG Card set using Scryfall API
    /// </summary>
    public class CardSet
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("search_uri")]
        public string SearchUri { get; set; }
        [JsonProperty("code")]
        public string Code { get; set; }
        [JsonProperty("released_at")]
        public string ReleasedAt { get; set; }
        [JsonProperty("icon_svg_uri")]
        public string IconSvgUri { get; set; }
        [JsonProperty("set_type")]
        public CardSetType SetType { get; set; }

        [JsonIgnore]
        public BitmapImage Icon
        {
            get
            {
                string path = File.Exists($"{IO.SetIconPath}{Code}.png") ? $"{IO.SetIconPath}{Code}.png" : "";
                if(path == "") { return null; }
                BitmapImage img = new(new Uri(Path.GetFullPath(path)));

                return img;
            }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum CardSetType
        {
            Core,
            Expansion,
            Masters,
            Masterpiece,
            From_the_vault,
            Spellbook,
            Premium_deck,
            Duel_deck,
            Draft_innovation,
            Treasure_chest,
            Commander,
            Planechase,
            Archenemy,
            Vanguard,
            Funny,
            Starter,
            Box,
            Promo,
            Token,
            Memorabilia
        }

        /// <summary>
        /// Returns array of all card set types
        /// </summary>
        public static CardSetType[] GetSetTypes()
        {
            return Enum.GetValues(typeof(CardSetType)).Cast<CardSetType>().ToArray();
        }
    }
    /// <summary>
    /// Scryfall API card data object
    /// </summary>
    public class CardData
    {
        [JsonProperty("data")]
        public List<Card> Data { get; set; }
        [JsonProperty("has_more")]
        public bool HasMore { get; set; }
        [JsonProperty("next_page")]
        public string NextPage { get; set; }
        [JsonProperty("total_cards")]
        public int TotalCards { get; set; }
    }
    /// <summary>
    /// Scryfall API card set data object
    /// </summary>
    public class CardSetData
    {
        [JsonProperty("data")]
        public List<CardSet> Data { get; set; }
    }
    /// <summary>
    /// Scryfall API card face object
    /// </summary>
    public class CardFace
    {
        [JsonProperty("colors")]
        public List<Card.CardColor> Colors { get; set; }
        [JsonProperty("image_uris")]
        public Dictionary<string, string> ImageUris { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("type_line")]
        public string TypeLine { get; set; }
    }
}