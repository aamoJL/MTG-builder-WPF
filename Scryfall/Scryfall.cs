using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace MTG.Scryfall
{
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
        public List<CardColors> ColorIdentity { get; set; }
        [JsonProperty("keywords")]
        public List<string> Keywords { get; set; }
        [JsonProperty("type_line")]
        public string TypeLine { get; set; }
        [JsonProperty("rarity")]
        public CardRarity Rarity { get; set; }
        [JsonProperty("set")]
        public string Set { get; set; }

        public BitmapImage PrimaryFace
        {
            get
            {
                if (ImageUris == null)
                {
                    if (CardFaces.Count > 1 && CardFaces[0].ImageUris != null)
                    {
                        BitmapImage bitmapImage = new();
                        bitmapImage.BeginInit();
                        bitmapImage.UriSource = new Uri(CardFaces[0].ImageUris["normal"]);
                        bitmapImage.EndInit();
                        return bitmapImage;
                    }

                    return null;
                }
                else
                {
                    BitmapImage bitmapImage = new();
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource = new Uri(ImageUris["normal"]);
                    bitmapImage.EndInit();
                    return bitmapImage;
                }
            }
        }
        public BitmapImage SecondaryFace
        {
            get
            {
                if (!HasTwoFaces) { return null; }
                else
                {
                    BitmapImage bitmapImage = new();
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource = new Uri(CardFaces[1].ImageUris["normal"]);
                    bitmapImage.EndInit();
                    return bitmapImage;
                }
            }
        }
        public bool HasTwoFaces => CardFaces != null;

        [JsonConverter(typeof(StringEnumConverter))]
        public enum CardColors
        {
            W, U, B, R, G
        }
        [JsonConverter(typeof(StringEnumConverter))]
        public enum CardRarity
        {
            common, uncommon, rare, special, mythic, bonus
        }
    }
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

        public string Icon => IconSvgUri;

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

        public static CardSetType[] GetSetTypes()
        {
            return Enum.GetValues(typeof(CardSetType)).Cast<CardSetType>().ToArray();
        }
    }

    public class CollectionCard
    {
        public virtual Card Card { get; set; }
        public virtual int Count { get; set; }

        public CollectionCard(Card card, int count = 1)
        {
            Card = card;
            Count = count;
        }
    }
    public class CardCollection
    {
        public string Name { get; set; }
        public bool UnsavedChanges { get; set; }
        public ObservableCollection<ListBoxCollectionCard> Cards { get; }

        public CardCollection()
        {
            Cards = new ObservableCollection<ListBoxCollectionCard>();
            Name = "";
        }

        public void Clear()
        {
            UnsavedChanges = true;
            Cards.Clear();
        }
        public void AddCard(Card card)
        {
            foreach (CollectionCard collectionCard in Cards)
            {
                if (collectionCard.Card.Id == card.Id)
                {
                    collectionCard.Count++;
                    UnsavedChanges = true;
                    return;
                }
            }

            Cards.Add(new ListBoxCollectionCard(card));
            UnsavedChanges = true;
        }
        public void AddCard(CollectionCard card)
        {
            foreach (CollectionCard collectionCard in Cards)
            {
                if (collectionCard.Card.Id == card.Card.Id)
                {
                    collectionCard.Count++;
                    UnsavedChanges = true;
                    return;
                }
            }

            Cards.Add(new ListBoxCollectionCard(card));
            UnsavedChanges = true;
        }
        public void RemoveCard(CollectionCard card)
        {
            for (int i = 0; i < Cards.Count; i++)
            {
                CollectionCard collectionCard = Cards[i];

                if (collectionCard.Card.Id == card.Card.Id)
                {
                    collectionCard.Count--;
                    if (collectionCard.Count <= 0)
                    {
                        Cards.RemoveAt(i);
                    }
                    UnsavedChanges = true;
                    break;
                }
            }
        }
        public void ChangeCollection(List<CollectionCard> cards, string name)
        {
            Name = name;
            Cards.Clear();
            for (int i = 0; i < cards.Count; i++)
            {
                Cards.Add(new ListBoxCollectionCard(cards[i]));
            }

            UnsavedChanges = false;
        }
        public void Save(string path)
        {
            if (Name == "" || path == "") { return; }
            SaveCollectionToFile(path);
            UnsavedChanges = false;
        }

        private void SaveCollectionToFile(string path)
        {
            using StreamWriter file = File.CreateText(path);
            JsonSerializer serializer = new();
            serializer.Serialize(file, Cards);
        }
    }

    public class CardData
    {
        [JsonProperty("data")]
        public List<Card> Data { get; set; }
    }
    public class CardSetData
    {
        [JsonProperty("data")]
        public List<CardSet> Data { get; set; }
    }

    public class ListBoxCollectionCard : CollectionCard, INotifyPropertyChanged
    {
        public ListBoxCollectionCard(Card card, int count = 1) : base(card, count) { }
        public ListBoxCollectionCard(CollectionCard card) : base(card.Card, card.Count) { }

        public override int Count
        {
            get => base.Count;
            set
            {
                base.Count = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CardFace
    {
        [JsonProperty("colors")]
        public List<Card.CardColors> Colors { get; set; }
        [JsonProperty("image_uris")]
        public Dictionary<string, string> ImageUris { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("type_line")]
        public string TypeLine { get; set; }
    }
}