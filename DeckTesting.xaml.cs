using Microsoft.Win32;
using MTG.Scryfall;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MTG_builder
{
    /// <summary>
    /// Interaction logic for DeckTesting.xaml
    /// </summary>
    public partial class DeckTesting : Window
    {
        private bool imageDrag;
        private bool deckDrag;
        private Point imageDragPivot;
        private Image imageDragObject;
        private Grid deckImageDragObject;
        private readonly List<DependencyObject> hitTestResults = new();

        private readonly string collectionsPath = "Resources/Collections/";
        private readonly float canvasScaleFactor = 1.1f;
        private readonly MatrixTransform _canvasTransform = new();

        private bool canvasPanning;
        private Point canvasPanningStartPoint;

        private readonly CardCollection deckOneCollection = new(); // Selected deck
        private readonly CardCollection deckTwoCollection = new(); // Selected deck
        private List<Card> deckOneCards = new(); // Gameplay cards
        private List<Card> deckTwoCards = new(); // Gameplay cards

        public DeckTesting()
        {
            InitializeComponent();
        }

        // Return the result of the hit test to the callback.
        public HitTestResultBehavior MyHitTestResult(HitTestResult result)
        {
            // Add the hit test result to the list that will be processed after the enumeration.
            hitTestResults.Add(result.VisualHit);

            // Set the behavior to return visuals at all z-order levels.
            return HitTestResultBehavior.Continue;
        }

        private void CanvasZoom(int delta, Point pivot)
        {
            float scaleFactor = canvasScaleFactor;

            // Limit zooming
            if (delta > 0 && _canvasTransform.Matrix.M11 > 1.7f)
            {
                return;
            }

            if (delta < 0 && _canvasTransform.Matrix.M11 < .3f)
            {
                return;
            }

            if (delta < 0)
            {
                scaleFactor = 1f / scaleFactor;
            }

            Matrix canvasMatrix = _canvasTransform.Matrix;
            canvasMatrix.ScaleAt(scaleFactor, scaleFactor, pivot.X, pivot.Y);
            _canvasTransform.Matrix = canvasMatrix;

            foreach (UIElement child in GameCanvas.Children)
            {
                Matrix scaleMatrix = (child.RenderTransform as MatrixTransform).Matrix;
                scaleMatrix.ScaleAt(scaleFactor, scaleFactor, pivot.X, pivot.Y);

                double x = Canvas.GetLeft(child);
                double y = Canvas.GetTop(child);

                double sx = x * scaleFactor;
                double sy = y * scaleFactor;

                Canvas.SetLeft(child, sx);
                Canvas.SetTop(child, sy);
                (child.RenderTransform as MatrixTransform).Matrix = scaleMatrix;
            }
        }
        private void CanvasStartPan(Point pivot)
        {
            canvasPanning = true;
            canvasPanningStartPoint = _canvasTransform.Inverse.Transform(pivot);
        }
        private void CanvasPan()
        {
            Point mousePosition = _canvasTransform.Inverse.Transform(Mouse.GetPosition(GameCanvas));
            Vector delta = Point.Subtract(mousePosition, canvasPanningStartPoint);

            foreach (UIElement child in GameCanvas.Children)
            {
                double x = Canvas.GetLeft(child);
                double y = Canvas.GetTop(child);

                double sx = x + (delta.X * _canvasTransform.Matrix.M11);
                double sy = y + (delta.Y * _canvasTransform.Matrix.M11);

                Canvas.SetLeft(child, sx);
                Canvas.SetTop(child, sy);
            }

            canvasPanningStartPoint = mousePosition;
        }
        private void CanvasEndPan()
        {
            canvasPanning = false;
        }
        private void CanvasRemoveElement(UIElement element)
        {
            GameCanvas.Children.Remove(element);
            //foreach (UIElementCollection item in GameCanvas.Children)
            //{
            //    GameCanvas.Children.remo
            //}
        }

        private void ImageStartDrag(Image img)
        {
            Panel.SetZIndex(img, 10);
            imageDragObject = img;
            imageDrag = true;
            Point imgPos = new(Canvas.GetLeft(img), Canvas.GetTop(img));
            imageDragPivot = (Point)(imgPos - Mouse.GetPosition(GameCanvas));
        }
        private void ImageDrag()
        {
            Point newPoint = Mouse.GetPosition(GameCanvas);
            Canvas.SetLeft(imageDragObject, newPoint.X + imageDragPivot.X);
            Canvas.SetTop(imageDragObject, newPoint.Y + imageDragPivot.Y);
        }
        private void ImageEndDrag(Point pivot)
        {
            hitTestResults.Clear();

            VisualTreeHelper.HitTest(GameCanvas, null,
                new HitTestResultCallback(MyHitTestResult),
                new PointHitTestParameters(pivot));

            // Perform actions on the hit test results list.
            if (hitTestResults.Count > 2)
            {
                foreach (DependencyObject item in hitTestResults)
                {
                    if(imageDragObject.DataContext is Card card)
                    {
                        string itemName = item.GetValue(NameProperty).ToString();
                        switch (itemName)
                        {
                            case "DeckOneTop":
                                PutCardToDeck(card, 1, false);
                                CanvasRemoveElement(imageDragObject);
                                break;
                            case "DeckOneBottom":
                                PutCardToDeck(card, 1, true);
                                CanvasRemoveElement(imageDragObject);
                                break;
                            case "DeckTwoTop":
                                PutCardToDeck(card, 2, false);
                                CanvasRemoveElement(imageDragObject);
                                break;
                            case "DeckTwoBottom":
                                PutCardToDeck(card, 2, true);
                                CanvasRemoveElement(imageDragObject);
                                break;
                            default:
                                break;
                        }
                    }
                }
                Panel.SetZIndex(imageDragObject, (int)hitTestResults[1].GetValue(Panel.ZIndexProperty) + 1);
            }
            else
            {
                Panel.SetZIndex(imageDragObject, 1);
            }

            imageDrag = false;
            imageDragObject = null;
        }
        
        private void DeckImageStartDrag(Grid grid)
        {
            deckImageDragObject = grid;
            deckDrag = true;
            Point imgPos = new(Canvas.GetLeft(grid), Canvas.GetTop(grid));
            Point mousePos = Mouse.GetPosition(GameCanvas);
            imageDragPivot = (Point)(imgPos - mousePos);
        }
        private void DeckDrag()
        {
            Point mousePos = Mouse.GetPosition(GameCanvas);
            Canvas.SetTop(deckImageDragObject, mousePos.Y + imageDragPivot.Y);
            Canvas.SetLeft(deckImageDragObject, mousePos.X + imageDragPivot.X);
        }

        private void DeckCardImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image img)
            {
                if (e.ClickCount == 2)
                {
                    if (img.DataContext is Card card)
                    {
                        SwapCardFace(img, card);
                    }
                }
                else
                {
                    ImageStartDrag(img);
                }
            }
            else if (sender is Grid grid)
            {
                DeckImageStartDrag(grid);
            }
        }
        private void DeckCardImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image card)
            {

                if ((card.DataContext as Card).Tapped)
                {
                    Matrix m = (card.RenderTransform as MatrixTransform).Matrix;
                    m.RotatePrepend(-90);
                    (card.RenderTransform as MatrixTransform).Matrix = m;
                    (card.DataContext as Card).Tapped = false;
                }
                else
                {
                    Matrix m = (card.RenderTransform as MatrixTransform).Matrix;
                    m.RotatePrepend(90);
                    (card.RenderTransform as MatrixTransform).Matrix = m;
                    (card.DataContext as Card).Tapped = true;
                }
            }
        }
        private void DeckCardImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if(sender is Image image && image.DataContext is Card card)
            {
                if (card.HasTwoFaces)
                {
                    image.Source = image.Source == card.PrimaryFace ? card.SecondaryFace : card.PrimaryFace;
                }
            }
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            CanvasZoom(e.Delta, e.GetPosition(GameCanvas));
        }
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (canvasPanning)
            {
                CanvasPan();
            }

            if (imageDrag)
            {
                ImageDrag();
            }

            if (deckDrag)
            {
                DeckDrag();
            }
        }
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                CanvasStartPan(Mouse.GetPosition(GameCanvas));
            }
        }
        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton != MouseButtonState.Pressed)
            {
                CanvasEndPan();
            }
        }
        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (imageDrag)
            {
                ImageEndDrag(Mouse.GetPosition(GameCanvas));
            }
            if (deckDrag)
            {
                deckDrag = false;
                imageDragObject = null;
            }
        }

        private void SelectDeck(string name, List<CollectionCard> cards, int deckNumber)
        {
            if(deckNumber == 1)
            {
                deckOneCollection.ChangeCollection(cards, name);
                GetDeckCards(1);
            }
            else if(deckNumber == 2)
            {
                deckTwoCollection.ChangeCollection(cards, name);
                GetDeckCards(2);
            }
            else { return; }
        }
        private void GetDeckCards(int deckNumber)
        {
            List<Card> cards = new();

            if (deckNumber == 1)
            {
                foreach (CollectionCard collectionCard in deckOneCollection.Cards)
                {
                    for (int i = 0; i < collectionCard.Count; i++)
                    {
                        cards.Add(collectionCard.Card);
                    }
                }
                deckOneCards = cards;
                DeckOneListBox.ItemsSource = deckOneCards;
            }
            else if (deckNumber == 2)
            {
                foreach (CollectionCard collectionCard in deckTwoCollection.Cards)
                {
                    for (int i = 0; i < collectionCard.Count; i++)
                    {
                        cards.Add(collectionCard.Card);
                    }
                }
                deckTwoCards = cards;
                DeckTwoListBox.ItemsSource = deckTwoCards;
            }
            else { return; }
        }
        private void DrawCard(int cardIndex, int deckNumber)
        {
            if(deckNumber == 3)
            {
                // Side
                Card card = (DeckSideListBox.SelectedItem as CollectionCard).Card;
                CreateCanvasCard(DeckOnePile, card);
            }
            else
            {
                List<Card> deck = deckNumber == 1 ? deckOneCards : deckTwoCards;
                if (deck.Count == 0) { return; }

                Card card = deck[cardIndex];
                deck.RemoveAt(cardIndex);

                if (card != null)
                {
                    Grid pile = deckNumber == 1 ? DeckOnePile : DeckTwoPile;
                    CreateCanvasCard(pile, card);
                }

                if(deckNumber == 1) { DeckOneListBox.Items.Refresh(); }
                else if (deckNumber == 2) { DeckTwoListBox.Items.Refresh(); }
            }
        }
        private void CreateCanvasCard(Grid pile, Card card)
        {
            Image cardImage = new();
            cardImage.Height = 250;
            cardImage.Source = card.PrimaryFace;
            cardImage.DataContext = card;

            cardImage.RenderTransformOrigin = new(.5f, .5f);

            MatrixTransform matrixTransform = new(_canvasTransform.Matrix);

            // Set card position
            double x = Canvas.GetLeft(pile);
            double y = Canvas.GetTop(pile);
            Canvas.SetTop(cardImage, y);
            Canvas.SetLeft(cardImage, x + ((DeckOnePile.ActualWidth + 20) * _canvasTransform.Matrix.M11));

            cardImage.RenderTransform = matrixTransform;

            cardImage.MouseLeftButtonDown += DeckCardImage_MouseLeftButtonDown;
            cardImage.MouseRightButtonDown += DeckCardImage_MouseRightButtonDown;

            _ = GameCanvas.Children.Add(cardImage);
        }

        private static void ShuffleDeck(List<Card> deck)
        {
            int count = deck.Count;
            int last = count - 1;
            for (int i = 0; i < last; ++i)
            {
                int r = new Random().Next(i, count);
                Card tmp = deck[i];
                deck[i] = deck[r];
                deck[r] = tmp;
            }
        }
        private static void SwapCardFace(Image img, Card card)
        {
            if (card.HasTwoFaces)
            {
                img.Source = img.Source.ToString() == card.CardFaces[0].ImageUris["normal"] ? card.SecondaryFace : card.PrimaryFace;
            }
        }
        private void PutCardToDeck(Card card, int deckNumber, bool toBottom)
        {
            List<Card> cards = deckNumber == 1 ? deckOneCards : deckTwoCards;
            if (toBottom)
            {
                cards.Add(card);
            }
            else
            {
                cards.Insert(0, card);
            }

            DeckOneListBox.Items.Refresh();
            DeckTwoListBox.Items.Refresh();
        }

        private void DeckOneOpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = IO.OpenFileDialog(collectionsPath);

            if (openFileDialog.ShowDialog() == true)
            {
                List<CollectionCard> collectionCards = IO.ReadCollectionFromFile(openFileDialog.FileName);
                SelectDeck(openFileDialog.SafeFileName, collectionCards, 1);
            }
        }
        private void DeckOneHideButton_Click(object sender, RoutedEventArgs e)
        {
            if(DeckOneListBox.Visibility == Visibility.Visible)
            {
                DeckOneListBox.Visibility = Visibility.Hidden;
                DeckOneHideButton.Content = "Show";
            }
            else
            {
                DeckOneListBox.Visibility = Visibility.Visible;
                DeckOneHideButton.Content = "Hide";
            }
        }
        private void DeckOneShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            ShuffleDeck(deckOneCards);
            DeckOneListBox.Items.Refresh();
        }
        private void DeckOneDrawButton_Click(object sender, RoutedEventArgs e)
        {
            if(deckOneCards.Count > 0)
            {
                DrawCard(0, 1);
            }
        }
        private void DeckOneDrawSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeckOneListBox.SelectedIndex != -1)
            {
                DrawCard(DeckOneListBox.SelectedIndex, 1);
            }
        }
        private void DeckTwoOpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = IO.OpenFileDialog(collectionsPath);

            if (openFileDialog.ShowDialog() == true)
            {
                List<CollectionCard> collectionCards = IO.ReadCollectionFromFile(openFileDialog.FileName);
                SelectDeck(openFileDialog.SafeFileName, collectionCards, 2);
            }
        }
        private void DeckTwoDrawButton_Click(object sender, RoutedEventArgs e)
        {
            if (deckTwoCards.Count > 0)
            {
                DrawCard(0, 2);
            }
        }
        private void DeckTwoDrawSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeckTwoListBox.SelectedIndex != -1)
            {
                DrawCard(DeckTwoListBox.SelectedIndex, 2);
            }
        }
        private void DeckTwoHideButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeckTwoListBox.Visibility == Visibility.Visible)
            {
                DeckTwoListBox.Visibility = Visibility.Hidden;
                DeckTwoHideButton.Content = "Show";
            }
            else
            {
                DeckTwoListBox.Visibility = Visibility.Visible;
                DeckTwoHideButton.Content = "Hide";
            }
        }
        private void DeckTwoShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            ShuffleDeck(deckTwoCards);
            DeckTwoListBox.Items.Refresh();
        }
        private void DeckSideDrawButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeckSideListBox.SelectedIndex != -1)
            {
                DrawCard(DeckSideListBox.SelectedIndex, 3);
            }
        }
        private void DeckSideOpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = IO.OpenFileDialog(collectionsPath);

            if (openFileDialog.ShowDialog() == true)
            {
                List<CollectionCard> collectionCards = IO.ReadCollectionFromFile(openFileDialog.FileName);
                DeckSideListBox.ItemsSource = collectionCards;
            }
        }

        private void MenuNewGame_Click(object sender, RoutedEventArgs e)
        {
            PlayerOneLife.Text = "20";
            PlayerTwoLife.Text = "20";

            GetDeckCards(1);
            GetDeckCards(2);

            ShuffleDeck(deckOneCards);
            ShuffleDeck(deckTwoCards);

            UIElement[] elements = new UIElement[2]
            {
                GameCanvas.Children[0],
                GameCanvas.Children[1]
            };

            GameCanvas.Children.Clear();

            _ = GameCanvas.Children.Add(elements[0]);
            _ = GameCanvas.Children.Add(elements[1]);

            for (int i = 0; i < 7; i++)
            {
                DrawCard(0, 1);
                DrawCard(0, 2);
            }
        }
    }
}

