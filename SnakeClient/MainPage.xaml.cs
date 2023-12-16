using System;
using System.Xml.Linq;
using Models;

namespace SnakeGame;

public partial class MainPage : ContentPage
{ 
    //GameController for View to get updates
    GameController.GameController gc;

    public MainPage()
    {
        InitializeComponent();

        //Initializes instance of game controller     
        gc = new GameController.GameController();

        //register handlers
        gc.DataArrived += OnFrame;
        gc.Error += NetworkErrorHandler;
        gc.Connected += registerName;
        graphicsView.Invalidate();
        
    }

    /// <summary>
    /// Event handlr for user keyboard inputs
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void OnTapped(object sender, EventArgs args)
    {
        keyboardHack.Focus();
    }

    /// <summary>
    /// Sends the corresponding move triggered by keyboard
    /// input to server. 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void OnTextChanged(object sender, TextChangedEventArgs args)
    {
        Entry entry = (Entry)sender;
        String text = entry.Text.ToLower();
        if (text == "w")
        {
            gc.MessageEntered("{\"moving\":\"up\"}");
        }
        else if (text == "a")
        {
            gc.MessageEntered("{\"moving\":\"left\"}");
        }
        else if (text == "s")
        {
            gc.MessageEntered("{\"moving\":\"down\"}");
        }
        else if (text == "d")
        {
            gc.MessageEntered("{\"moving\":\"right\"}");
        }
        entry.Text = "";
        
    }
    /// <summary>
    /// Send error message to user if there was an error connecting to network and we enable buttons.
    /// </summary>
    /// <param name="err"></param>
    private void NetworkErrorHandler(string err)
    {
        connectButton.IsEnabled = true;
        serverText.IsEnabled = true;
        nameText.IsEnabled = true;
        DisplayAlert("Error", err, "OK");
    }


    /// <summary>
    /// Event handler for the connect button
    /// Connection attempt interface  will be here in the view.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void ConnectClick(object sender, EventArgs args)
    {
        if (serverText.Text == "")
        {
            DisplayAlert("Error", "Please enter a server address", "OK");
            return;
        }
        if (nameText.Text == "")
        {
            DisplayAlert("Error", "Please enter a name", "OK");
            return;
        }
        if (nameText.Text.Length > 16)
        {
            DisplayAlert("Error", "Name must be less than 16 characters", "OK");
            return;
        }
        try
        {
            connectButton.IsEnabled = false;
            serverText.IsEnabled = false;
            nameText.IsEnabled = false;

            gc.Connect(serverText.Text);
        }
        catch (Exception)
        {
            DisplayAlert("Error", "Connection to server failed. Check that host name is correct", "OK");
            return;
        }
        keyboardHack.Focus();
    }

    /// <summary>
    /// Event handler for when the controller has updated the world
    /// </summary>
    public void OnFrame()
    {
        worldPanel.setWorld(gc.getWorld());
        Dispatcher.Dispatch(() => graphicsView.Invalidate());
    }


    /// <summary>
    /// Map buttons to their movements in the game 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ControlsButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("Controls",
                     "W:\t\t Move up\n" +
                     "A:\t\t Move left\n" +
                     "S:\t\t Move down\n" +
                     "D:\t\t Move right\n",
                     "OK");
    }
    /// <summary>
    /// Event handler to display about button if clicked.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AboutButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("About",
      "SnakeGame solution\nArtwork by Jolie Uk and Alex Smith\nGame design by Daniel Kopta and Travis Martin\n" +
      "Implementation by ...\n" +
        "CS 3500 Fall 2022, University of Utah", "OK");
    }

    private void ContentPage_Focused(object sender, FocusEventArgs e)
    {
        if (!connectButton.IsEnabled)
            keyboardHack.Focus();
    }


    /// <summary>
    /// Subscribes to ConnectedHandler event of GameController.
    /// Sends server name of user and server will begin sending data.
    /// </summary>
    private void registerName()
    {
        gc.MessageEntered(nameText.Text);
    }
}