using Microsoft.Win32;
using MTG.Scryfall;
using MTG_builder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace MTG
{
    /// <summary>
    /// Collection of MTG Cards
    /// </summary>
    public class CardCollection
    {
        public string Name { get; set; }
        public bool UnsavedChanges { get; private set; }
        public ObservableCollection<ListBoxCollectionCard> Cards { get; }
        public event EventHandler CollectionChanged;

        private string filePath = "";

        public CardCollection()
        {
            Cards = new ObservableCollection<ListBoxCollectionCard>();
            Name = "";
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
        public void AddCard(CollectionCard card, int count = 1)
        {
            foreach (CollectionCard collectionCard in Cards)
            {
                if (collectionCard.Card.Id == card.Card.Id)
                {
                    collectionCard.Count += count;
                    UnsavedChanges = true;
                    return;
                }
            }

            Cards.Add(new ListBoxCollectionCard(card, count));
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
                Cards.Add(new ListBoxCollectionCard(cards[i], cards[i].Count));
            }

            UnsavedChanges = false;

            CollectionChanged?.Invoke(this, EventArgs.Empty);
        }
        public void SaveAsWithDialog(string defaultName = "NewCollection")
        {
            SaveFileDialog saveFileDialog = IO.SaveFileDialog(IO.CollectionsPath, defaultName);
            bool? result = saveFileDialog.ShowDialog();

            // Process save file dialog box results
            if (result == true && !string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(saveFileDialog.SafeFileName)))
            {
                // Save document
                filePath = saveFileDialog.FileName;
                Name = Path.GetFileNameWithoutExtension(saveFileDialog.SafeFileName);
                Save();
                CollectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public void Save()
        {
            if (Name == "" || filePath == "")
            {
                SaveAsWithDialog();
            }
            else if (filePath != "")
            {
                Save(filePath);
            }
        }
        public void Save(string path)
        {
            SaveCollectionToFile(path);
        }
        public void Sort()
        {
            ChangeCollection(Cards.OrderBy(x => x.Card.GetColorIdentity).ThenBy(x => x.Card.CMC).Cast<CollectionCard>().ToList(), Name);
            UnsavedChanges = true;
        }
        public void ChangeCollectionFromFile(string path)
        {
            List<CollectionCard> cards = IO.ReadCollectionFromFile(path);
            string collectionsName = Path.GetFileNameWithoutExtension(path);
            filePath = path;

            ChangeCollection(cards, collectionsName);
        }
        public void FilterCollection(List<Card.CardColor> colorFilters)
        {
            foreach (ListBoxCollectionCard card in Cards)
            {
                if (card.Card.GetColorIdentity != Card.CardColor.Multicolor)
                {
                    card.Visible = !colorFilters.Contains(card.Card.GetColorIdentity);
                }
                else
                {
                    card.Visible = true;
                    foreach (Card.CardColor filter in colorFilters)
                    {
                        if (card.Card.ColorIdentity.Contains(filter))
                        {
                            card.Visible = false;
                            break;
                        }
                    }
                }
            }
        }

        private void SaveCollectionToFile(string path)
        {
            IO.SaveJsonToFile(path, Cards);
            UnsavedChanges = false;
        }
    }
    /// <summary>
    /// Card object for CardCollection class
    /// </summary>
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
    /// <summary>
    /// CollectionCard that can be used in ListBoxes
    /// </summary>
    public class ListBoxCollectionCard : CollectionCard, INotifyPropertyChanged
    {
        public ListBoxCollectionCard(Card card, int count = 1) : base(card, count) { Visible = true; }
        public ListBoxCollectionCard(CollectionCard card, int count = 1) : base(card.Card, count) { Visible = true; }

        private bool visible = true;

        public override int Count
        {
            get => base.Count;
            set
            {
                base.Count = value;
                OnPropertyChanged();
            }
        }
        public bool Visible { get => visible; set { visible = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}