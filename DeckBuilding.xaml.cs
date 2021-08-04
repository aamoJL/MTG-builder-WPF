using Microsoft.Win32;
using MTG.Scryfall;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MTG_builder;
using System.Globalization;
using System.IO;

namespace MTG
{
    /// <summary>
    /// Interaction logic for DeckBuilding.xaml
    /// </summary>
    public partial class DeckBuilding : Window
    {
        private readonly CardCollection primaryCardCollection = new();
        private readonly CardCollection secondaryCardCollection = new();
        private readonly CardCollection setCardCollection = new();

        public DeckBuilding()
        {
            InitializeComponent();
            IO.InitDirectories();
            IO.UpdateSetLists();

            // Subscribe to events
            primaryCardCollection.CollectionChanged += PrimaryCardCollection_CollectionChanged;

            // Add item sources
            PrimaryCollectionListBox.ItemsSource = primaryCardCollection.Cards;
            SecondaryCollectionListBox.ItemsSource = secondaryCardCollection.Cards;
            CardSetsComboBox.ItemsSource = IO.GetCardSets();
            CardCollectionsComboBox.ItemsSource = IO.GetJsonFileNames(IO.CollectionsPath);
            CardSetTypeComboBox.ItemsSource = CardSet.GetSetTypes();
            CardSetImageListBox.ItemsSource = setCardCollection.Cards;

            // Set cardset type combobox's selected item to "Expansion"
            CardSetTypeComboBox.SelectedIndex = 1;
        }

        #region Subscribed Events
        private void PrimaryCardCollection_CollectionChanged(object sender, EventArgs e)
        {
            // Change primary collection name to textblock
            CollectionTextBlock.Text = primaryCardCollection.Name != "" ? primaryCardCollection.Name : "Unsaved Collection";
        }
        #endregion

        #region Tab Control Events
        private void CardSetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CardSetsComboBox.SelectedIndex == -1) { return; }

            // Set card set listbox items to selected set's cards
            CardSet set = CardSetsComboBox.SelectedItem as CardSet;
            List<Card> cards = ScryfallAPI.FetchScryfallSetCards(set.SearchUri);

            LoadCardSetImagesAsync(cards);
        }
        private void CardCollectionsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CardCollectionsComboBox.SelectedIndex == -1) { return; }

            // Change secondary collection to selected collection
            string collectionName = CardCollectionsComboBox.SelectedItem.ToString();

            LoadCardCollectionImagesAsync(collectionName);
        }
        private void CardSetTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Change cardset items to selected type
            CardSetsComboBox.ItemsSource = GetFilteredCardSetList(IO.GetCardSets(), (CardSet.CardSetType)CardSetTypeComboBox.SelectedItem);
        }
        private void CardCollectionsComboBox_DropDownOpened(object sender, EventArgs e)
        {
            // Get card collection names
            CardCollectionsComboBox.ItemsSource = IO.GetJsonFileNames(IO.CollectionsPath);
        }
        #endregion

        #region Listbox Events
        private void PrimaryCollectionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Change collection image display to selected card's image
            PrimaryCollectionHoverImage.Source = ((CollectionCard)PrimaryCollectionListBox.SelectedItem)?.Card.PrimaryFace;
        }
        private void SecondaryCollectionCardImage_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //Only double left click
            if (e.ChangedButton != MouseButton.Left) { return; }

            // Add card to primary collection when secondary collection's card has been double clicked
            if ((sender as ListBoxItem).DataContext is CollectionCard collectionCard)
            {
                primaryCardCollection.AddCard(collectionCard);
            }
            else if ((sender as ListBoxItem).DataContext is Card card)
            {
                primaryCardCollection.AddCard(card);
            }
        }
        private void CardImage_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Change card image to card's flip side if card has two faces
            if (sender is Image img)
            {
                if (img.DataContext is Card card)
                {
                    if (card.HasTwoFaces)
                    {
                        string currentSource = img.Source.ToString(new CultureInfo("en-us"));
                        img.Source = card.PrimaryFace.ToString(new CultureInfo("en-us")) == currentSource ? card.SecondaryFace : card.PrimaryFace;
                    }
                }
                else if (img.DataContext is CollectionCard collectionCard)
                {
                    if (collectionCard.Card.HasTwoFaces)
                    {
                        string currentSource = img.Source.ToString(new CultureInfo("en-us"));
                        img.Source = collectionCard.Card.PrimaryFace.ToString(new CultureInfo("en-us")) == currentSource ? collectionCard.Card.SecondaryFace : collectionCard.Card.PrimaryFace;
                    }
                }
            }
        }
        private void PrimaryCollectionCard_MouseEnter(object sender, MouseEventArgs e)
        {
            // Show display image of the card when mouse is over it
            if (((ListBoxItem)sender).DataContext is CollectionCard collectionCard)
            {
                PrimaryCollectionHoverImage.Source = collectionCard.Card.PrimaryFace;
            }
        }
        private void CollectionCard_MouseLeave(object sender, MouseEventArgs e)
        {
            // Hide card display image when mouse leaves the card
            PrimaryCollectionHoverImage.Source = ((CollectionCard)PrimaryCollectionListBox.SelectedItem)?.Card.PrimaryFace;
        }
        #endregion

        #region Menu item Events
        private void DeckTestingMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Open deck testing window
            DeckTesting deckTestingWindow = new();
            deckTestingWindow.Show();
        }
        private void UpdateMenuIconsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            IO.DownloadAndConvertSetIconSVGs();
        }
        private void UpdateSetListMenuItem_Click(object sender, RoutedEventArgs e)
        {
            IO.UpdateSetLists();

            CardSetsComboBox.ItemsSource = IO.GetCardSets();
        }
        #endregion

        #region Primary Collection Events
        private void PrimaryCollectionSaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            primaryCardCollection.SaveAsWithDialog("CopyCollection");
        }
        private void PrimaryCollectionOpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (primaryCardCollection.UnsavedChanges)
            {
                MessageBoxResult message = IO.UnsavedChangesDialog();
                switch (message)
                {
                    case MessageBoxResult.Cancel:
                        return;
                    case MessageBoxResult.Yes:
                        primaryCardCollection.Save();
                        break;
                    case MessageBoxResult.No:
                    case MessageBoxResult.None:
                    case MessageBoxResult.OK:
                    default:
                        break;
                }
            }

            OpenFileDialog openFileDialog = IO.OpenFileDialog(IO.CollectionsPath);

            if (openFileDialog.ShowDialog() == true)
            {
                primaryCardCollection.LoadCollectionFromFile(openFileDialog.FileName);
            }
        }
        private void PrimaryCollectionNewButton_Click(object sender, RoutedEventArgs e)
        {
            if (primaryCardCollection.UnsavedChanges)
            {
                MessageBoxResult message = IO.UnsavedChangesDialog();
                switch (message)
                {
                    case MessageBoxResult.Cancel:
                        return;
                    case MessageBoxResult.Yes:
                        primaryCardCollection.Save();
                        break;
                    case MessageBoxResult.No:
                    case MessageBoxResult.None:
                    case MessageBoxResult.OK:
                    default:
                        break;
                }
            }

            primaryCardCollection.LoadCollection(new List<CollectionCard>(), "");
        }
        private void PrimaryCollectionSaveButton_Click(object sender, RoutedEventArgs e)
        {
            primaryCardCollection.Save();
        }
        private void PrimaryCollectionRemoveCardButton_Click(object sender, RoutedEventArgs e)
        {
            if (PrimaryCollectionListBox.SelectedItem is CollectionCard card)
            {
                primaryCardCollection.RemoveCard(card);
            }
        }
        private void PrimaryCollectionAddCardButton_Click(object sender, RoutedEventArgs e)
        {
            if (PrimaryCollectionListBox.SelectedItem is CollectionCard card)
            {
                primaryCardCollection.AddCard(card);
            }
        }
        #endregion

        #region Secondary Collection Events
        private void SecondaryCollectionAddCardButton_Click(object sender, RoutedEventArgs e)
        {
            if (SecondaryCollectionListBox.SelectedItem is CollectionCard card)
            {
                secondaryCardCollection.AddCard(card);
            }
        }
        private void SecondaryCollectionSortButton_Click(object sender, RoutedEventArgs e)
        {
            secondaryCardCollection.Sort();
        }
        private void SecondaryCollectionColorFilterCheck_Click(object sender, RoutedEventArgs e)
        {
            secondaryCardCollection.FilterCollection(GetCollectionColorFilters());
            setCardCollection.FilterCollection(GetCollectionColorFilters());
        }
        private void SecondaryCollectionSaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!secondaryCardCollection.Loaded) { return; }
            secondaryCardCollection.Save();
        }
        private void CollectionMoveCardRightButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = PrimaryCollectionListBox.SelectedIndex;
            if (selectedIndex == -1 || secondaryCardCollection.Loaded) { return; }

            // Move card from primary to secondary collection
            SwapCardBetweenCollections(primaryCardCollection, secondaryCardCollection, selectedIndex);
        }
        private void CollectionMoveCardLeftButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = SecondaryCollectionListBox.SelectedIndex;
            if (selectedIndex == -1) { return; }

            // Move card from secondary to primary collection
            SwapCardBetweenCollections(secondaryCardCollection, primaryCardCollection, selectedIndex);
        }
        #endregion

        /// <summary>
        /// Returns active color filters for the secondary card collection
        /// </summary>
        /// <returns>List of active color filters</returns>
        private List<Card.CardColor> GetCollectionColorFilters()
        {
            List<Card.CardColor> colorFilters = new();
            if (WhiteCheck.IsChecked == false) { colorFilters.Add(Card.CardColor.W); }
            if (BlueCheck.IsChecked == false) { colorFilters.Add(Card.CardColor.U); }
            if (BlackCheck.IsChecked == false) { colorFilters.Add(Card.CardColor.B); }
            if (RedCheck.IsChecked == false) { colorFilters.Add(Card.CardColor.R); }
            if (GreenCheck.IsChecked == false) { colorFilters.Add(Card.CardColor.G); }
            if (ColorlessCheck.IsChecked == false) { colorFilters.Add(Card.CardColor.Colorless); }
            return colorFilters;
        }
        /// <summary>
        /// Returns list of card sets with the given type
        /// </summary>
        /// <param name="setList">List of card sets</param>
        /// <param name="type">Accepted type</param>
        /// <returns></returns>
        private static List<CardSet> GetFilteredCardSetList(List<CardSet> setList, CardSet.CardSetType type)
        {
            return setList.FindAll(x => x.SetType == type);
        }
        /// <summary>
        /// Moves a card between collections
        /// </summary>
        /// <param name="fromCollection"></param>
        /// <param name="toCollection"></param>
        /// <param name="fromIndex">Card's index in the fromCollection</param>
        private static void SwapCardBetweenCollections(CardCollection fromCollection, CardCollection toCollection, int fromIndex)
        {
            CollectionCard card = fromCollection.Cards[fromIndex];

            toCollection.AddCard(card);
            fromCollection.RemoveCard(card);
        }

        private async void LoadCardSetImagesAsync(List<Card> cards)
        {
            CardSetImageListBox.Visibility = Visibility.Hidden;
            CardSetLoadingTextBlock.Visibility = Visibility.Visible;

            await ScryfallAPI.DownloadCardImages(cards);

            // Change set cards to collection cards
            List<CollectionCard> collectionCards = new();

            for (int i = 0; i < cards.Count; i++)
            {
                collectionCards.Add(new ListBoxCollectionCard(cards[i]));
            }

            setCardCollection.LoadCollection(collectionCards, "");
            setCardCollection.FilterCollection(GetCollectionColorFilters());

            CardSetImageListBox.Visibility = Visibility.Visible;
            CardSetLoadingTextBlock.Visibility = Visibility.Collapsed;
        }
        private async void LoadCardCollectionImagesAsync(string collectionName)
        {
            SecondaryCollectionListBox.Visibility = Visibility.Hidden;
            SecondaryCollectionLoadingTextBlock.Visibility = Visibility.Visible;

            string collectionPath = $"{IO.CollectionsPath}{collectionName}.json";
            if (File.Exists(collectionPath))
            {
                List<CollectionCard> collectionCards = IO.ReadCollectionFromFile(collectionPath);

                await ScryfallAPI.DownloadCardImages(collectionCards);

                secondaryCardCollection.LoadCollectionFromFile(collectionPath);

                // Filter unselected colors
                secondaryCardCollection.FilterCollection(GetCollectionColorFilters());

                SecondaryCollectionListBox.Visibility = Visibility.Visible;
                SecondaryCollectionLoadingTextBlock.Visibility = Visibility.Collapsed;
            }
        }
    }
}