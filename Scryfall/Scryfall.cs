using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;

namespace MTG.Scryfall
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Card
    {
        public string id { get; set; }
        public string name { get; set; }
        public Dictionary<string, string> image_uris { get; set; }
        public string image_status { get; set; }
        public int cardmarket_id { get; set; }
        public List<CardFace> card_faces { get; set; }
        public decimal cmc { get; set; }
        public List<CardColors> color_identity { get; set; }
        public List<string> keywords { get; set; }
        public string type_line { get; set; }
        public CardRarity rarity { get; set; }
        public string set { get; set; }

        public BitmapImage PrimaryFace
        {
            get
            {
                if (image_uris == null)
                {
                    if (card_faces.Count > 1 && card_faces[0].image_uris != null)
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.UriSource = new Uri(card_faces[0].image_uris["normal"]);
                        bitmapImage.EndInit();
                        return bitmapImage;
                    }

                    return null;
                }
                else
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource = new Uri(image_uris["normal"]);
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
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource = new Uri(card_faces[1].image_uris["normal"]);
                    bitmapImage.EndInit();
                    return bitmapImage;
                }
            }
        }
        public bool HasTwoFaces => card_faces != null;
    }
    public class CardSet
    {
        public string name { get; set; }
        public string search_uri { get; set; }
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
        private readonly ObservableCollection<ListBoxCollectionCard> _cards;

        public string Name { get; set; }
        public bool UnsavedChanges { get; set; }
        public ObservableCollection<ListBoxCollectionCard> Cards { get { return _cards; } }

        public CardCollection()
        {
            _cards = new ObservableCollection<ListBoxCollectionCard>();
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
                if (collectionCard.Card.id == card.id)
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
                if (collectionCard.Card.id == card.Card.id)
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

                if (collectionCard.Card.id == card.Card.id)
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

        public void Save(string path)
        {
            if (Name == "" || path == "") { return; }
            SaveCollectionToFile(path);
            UnsavedChanges = false;
        }

        public void ChangeCollection(List<CollectionCard> cards, string name)
        {
            Name = name;
            Cards.Clear();
            for (int i = 0; i < cards.Count; i++)
            {
                _cards.Add(new ListBoxCollectionCard(cards[i]));
            }

            UnsavedChanges = false;
        }

        private void SaveCollectionToFile(string path)
        {
            using (StreamWriter file = File.CreateText(path))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, Cards);
            }
        }
    }

    public class CardData
    {
        public List<Card> data { get; set; }

        public string asd()
        {
            return "asd";
        }
    }
    public class CardSetData
    {
        public List<CardSet> data { get; set; }
    }

    public class ListBoxCollectionCard : CollectionCard, INotifyPropertyChanged
    {
        public ListBoxCollectionCard(Card card, int count = 1) : base(card, count)
        {
        }

        public ListBoxCollectionCard(CollectionCard card) : base(card.Card, card.Count)
        {
        }

        public override int Count
        {
            get
            {
                return base.Count;
            }
            set
            {
                base.Count = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class CardFace
    {
        public List<CardColors> colors { get; set; }
        public Dictionary<string, string> image_uris { get; set; }
        public string name { get; set; }
        public string type_line { get; set; }
    }

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