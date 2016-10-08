using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Net;
using Android.Net.Wifi.P2p;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace BCTTT
{
    public class Game
    {
        public Dictionary<string, string> Board { get; set; }
        public string PlayersTurn { get; private set; }

        public Game()
        {
            Board = new Dictionary<string, string> ();
            Board ["upper-left"] = "";
            Board ["upper-center"] = "";
            Board ["upper-right"] = "";
            Board ["middle-left"] = "";
            Board ["middle-center"] = "";
            Board ["middle-right"] = "";
            Board ["lower-left"] = "";
            Board ["lower-center"] = "";
            Board ["lower-right"] = "";

            PlayersTurn = "X"; // X always goes first
        }

        public bool GameOver() {
            // Do we have a winner?
            if (Winner () != null) {
                return true;
            }

            // Are all the squares full?
            foreach (var loc in Board.Keys) {
                if (Board [loc] == "")
                    return false;
            }

            // No winner, but we're full => Yep, we're done
            return true;
        }
        public string Winner() {
            string[] players = { "X", "O" };
            foreach (string player in players) {
                // Check the two diagonals first
                if (Board["upper-left"] == player &&
                    Board["middle-center"] == player &&
                    Board["lower-right"] == player)
                    return player;

                if (Board["upper-right"] == player &&
                    Board["middle-center"] == player &&
                    Board["lower-left"] == player)
                    return player;

                string[] horizontals = {"upper", "middle", "lower"};
                foreach (string horiz in horizontals) {
                    if (Board[horiz + "-right"] == player &&
                        Board[horiz + "-center"] == player &&
                        Board[horiz + "-left"] == player)
                        return player;
                }

                string[] verticals = {"left", "center", "right"};
                foreach (string vert in verticals) {
                    if (Board["upper-" + vert] == player &&
                        Board["middle-" + vert] == player &&
                        Board["lower-" + vert] == player)
                        return player;
                }
            }

            // No winner--return null
            return null;
        }

        /// <summary>
        ///  Player 'player' wants to move a piece to 'location'; is this legal?
        /// </summary>
        /// <returns><c>true</c>, if the move is legal, <c>false</c> otherwise.</returns>
        /// <param name="player">Player representation ("X" or "O").</param>
        /// <param name="location">Location ("upper-left", "middle-center", etc).</param>
        public bool PlayerGoes(string player, string location) {
            // TODO: Check that it's the right player's move
            if (player != PlayersTurn)
                return false;

            // Is that spot empty?
            if (Board [location] != "")
                return false;

            Board [location] = player;
            PlayersTurn = (player == "X" ? "O" : "X");
            return true;
        }
    }


    [Activity (Label = "BCTTT", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        public const string Tag = "MainActivity";

        Game game = new Game();
        Dictionary<string, Button> buttons = new Dictionary<string, Button> ();
        TextView messageView;

        private WifiP2pManager _manager;
        private WifiP2pManager.Channel _channel;
        private readonly IntentFilter _intentFilter = new IntentFilter();
        private WiFiDirectBroadcastReceiver _receiver;

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

            // Set our view from the "main" layout resource
            SetContentView (Resource.Layout.Main);

            _manager = (WifiP2pManager) GetSystemService (Activity.WifiP2pService);
            _channel = _manager.Initialize(this, MainLooper, null);

            // Set up the intent filter for the broadcast receiver
            _intentFilter.AddAction(WifiP2pManager.WifiP2pStateChangedAction);
            _intentFilter.AddAction(WifiP2pManager.WifiP2pPeersChangedAction);
            _intentFilter.AddAction(WifiP2pManager.WifiP2pConnectionChangedAction);
            _intentFilter.AddAction(WifiP2pManager.WifiP2pThisDeviceChangedAction);

            // Get our button from the layout resource,
            // and attach an event to it
            buttons["upper-left"] = FindViewById<Button>(Resource.Id.button1);
            buttons["upper-center"] = FindViewById<Button>(Resource.Id.button2);
            buttons["upper-right"] = FindViewById<Button>(Resource.Id.button3);
            buttons["middle-left"] = FindViewById<Button>(Resource.Id.button4);
            buttons["middle-center"] = FindViewById<Button>(Resource.Id.button5);
            buttons["middle-right"] = FindViewById<Button>(Resource.Id.button6);
            buttons["lower-left"] = FindViewById<Button>(Resource.Id.button7);
            buttons["lower-center"] = FindViewById<Button>(Resource.Id.button8);
            buttons["lower-right"] = FindViewById<Button>(Resource.Id.button9);

            messageView = FindViewById<TextView> (Resource.Id.textView1);
            messageView.Text = "Welcome to Android Tic-Tac-Toe!";

            MatchUIToBoard ();
            messageView.Text = string.Format ("It's {0}'s turn",
                game.PlayersTurn);

            foreach (var k in buttons.Keys) {
                buttons [k].Tag = k;
                Log.Info ("BCTTT", string.Format ("button[{0}].Tag = {1}", k, buttons[k].Tag));
                buttons [k].Click += ButtonClick;
            }
        }

        protected override void OnResume ()
        {
            base.OnResume ();

            _receiver = new WiFiDirectBroadcastReceiver (_manager, _channel, this);
            RegisterReceiver (_receiver, _intentFilter);

            _manager.DiscoverPeers(_channel, null);
        }
        protected override void OnPause ()
        {
            base.OnPause ();

            UnregisterReceiver (_receiver);
        }

        private void ButtonClick(object sender, EventArgs args) {
            Button button = (Button) sender;
            string buttonLocation = (string)button.Tag;

            Log.Info ("BCTTT", string.Format ("Player {0} moved at {1}", 
                game.PlayersTurn, buttonLocation));

            if (game.PlayerGoes (game.PlayersTurn, buttonLocation)) {
                MatchUIToBoard ();

                if (game.GameOver ()) {
                    if (game.Winner () != null) {
                        Toast.MakeText (this, 
                            string.Format ("{0} has won the game!", game.Winner ()), 
                            ToastLength.Short).Show ();
                    } else {
                        Toast.MakeText (this,
                            string.Format ("We have a tie game"),
                            ToastLength.Short).Show ();
                    }
                } 
                else {
                    this.messageView.Text = string.Format ("It is now {0}'s move", game.PlayersTurn);
                }
            } else {
                Toast.MakeText (this, "ILLEGAL MOVE! BAD PLAYER!", ToastLength.Short).Show ();
            }
        }

        private void MatchUIToBoard() {
            string[] horizontals = { "upper", "middle", "lower" };
            string[] verticals = { "left", "center", "right" };
            foreach (var h in horizontals)
                foreach (var v in verticals) {
                    buttons [h + "-" + v].Text = game.Board [h + "-" + v];
                    if (game.Board [h + "-" + v] != "")
                        buttons [h + "-" + v].Enabled = false;
                    else
                        buttons [h + "-" + v].Enabled = true;
                }
        }
    }

    public class PeerListListener : Java.Lang.Object, WifiP2pManager.IPeerListListener
    {
        public void OnPeersAvailable(WifiP2pDeviceList peers) {
            Log.Info ("PeerListListener", "OnPeersAvailable called");
            foreach (var peer in peers.DeviceList) {
                Log.Debug ("PeerListListener", string.Format ("Found {0}", peer.DeviceName));
            }
        }
    }


    /// <summary>
    /// A BroadcastReceiver that notifies of important wifi p2p events.
    /// </summary>
    public class WiFiDirectBroadcastReceiver : BroadcastReceiver
    {
        private readonly WifiP2pManager _manager;
        private readonly WifiP2pManager.Channel _channel;
        private readonly MainActivity _activity;
        private readonly PeerListListener _listener = new PeerListListener();

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="manager">WifiP2pManager system service</param>
        /// <param name="channel">Wifi p2p channel</param>
        /// <param name="activity">activity associated with the receiver</param>
        public WiFiDirectBroadcastReceiver(WifiP2pManager manager, WifiP2pManager.Channel channel,
            MainActivity activity)
        {
            _manager = manager;
            _channel = channel;
            _activity = activity;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            Log.Info ("BroadcastReceiver", "P2PBroadcastReceiver got " + intent);
            var action = intent.Action;

            if (WifiP2pManager.WifiP2pStateChangedAction.Equals(action))
            {
                // UI update to indicate wifi p2p status.
                var state = intent.GetIntExtra(WifiP2pManager.ExtraWifiState, -1);
                if (state == (int) WifiP2pState.Enabled) {
                    // Wifi Direct mode is enabled
                    //_activity.IsWifiP2PEnabled = true;
                    Toast.MakeText(context, "WiFiDirect IS enabled", ToastLength.Short).Show();
                }
                else
                {
                    Toast.MakeText (context, "WiFiDirect is not enabled", ToastLength.Short).Show ();
                }
                Log.Debug(MainActivity.Tag, "P2P state changed - " + state);
            }
            else if (WifiP2pManager.WifiP2pPeersChangedAction.Equals(action))
            {
                // request available peers from the wifi p2p manager. This is an
                // asynchronous call and the calling activity is notified with a
                // callback on PeerListListener.onPeersAvailable()
                _manager.RequestPeers (_channel, _listener);
                Log.Debug(MainActivity.Tag, "P2P peers changed");
            }
            else if (WifiP2pManager.WifiP2pConnectionChangedAction.Equals(action))
            {
                if (_manager == null)
                    return;

                var networkInfo = (NetworkInfo) intent.GetParcelableExtra(WifiP2pManager.ExtraNetworkInfo);

                if (networkInfo.IsConnected)
                {
                    // we are connected with the other device, request connection
                    // info to find group owner IP
                    //var fragment =
                    //    _activity.FragmentManager.FindFragmentById<DeviceDetailFragment>(Resource.Id.frag_detail);
                    //_manager.RequestConnectionInfo(_channel, fragment);
                }
                else
                {
                    // It's a disconnect
                    //_activity.ResetData();
                }
            }
        }
    }
}


