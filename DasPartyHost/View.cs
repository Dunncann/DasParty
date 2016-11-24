﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using DasPartyHost.Models;
using DasPartyHost.Spotify;
using DasPartyHost.Utils;
using DasPartyPersistence;
using DasPartyPersistence.Models;
using SpotifyAPI.Local.Enums;
using SpotifyAPI.Web.Models;
using Timer = System.Timers.Timer;

namespace DasPartyHost
{
    public partial class View : Form
    {
        private readonly Client _client;
        private readonly WebAPI _web;

        private readonly Playlist _playlist;

        private const string UserID = "9b9e4883-3d06-462f-ae16-324968aab4f6";

        public View()
        {
            InitializeComponent();
            
            // Connect to the client:
            _client = new Client(this);
            UpdateTrackView();
            UpdatePlayButton();

            // Connect to the web API:
            _web = new WebAPI();

            // Connect to the Rethink database
            _playlist = Playlist.Get(UserID);

            InitPlaylist();
        }

        public void UpdateTrackView()
        {
            var track = _client.Status.Track;
            this.InvokeIfRequired(() =>
            {
                if (track != null)
                {
                    nowPlaying.Text = track.ArtistResource.Name + " - " + track.TrackResource.Name;
                    nowPlayingImage.Image = track.GetAlbumArt(AlbumArtSize.Size160);
                }
                else
                {
                    nowPlaying.Text = "Nothing";
                    nowPlayingImage.Image = null;
                }
            });
        }

        public void UpdatePlayButton() => playBtn.Text = _client.Playing ? "Pause" : "Play";

        #region Play buttons

        private void playBtn_Click(object sender, EventArgs e) => _client.Playing = !_client.Playing;

        private void skipBtn_Click(object sender, EventArgs e)
        {
            _client.Skip();
            playBtn.Focus();
        }

        #endregion

        #region Track search

        private Timer _searchTimer;

        private void searchInput_TextChanged(object sender, EventArgs e)
        {
            // Executes after 500ms of not typing
            _searchTimer?.Close();
            _searchTimer = new Timer(500) {AutoReset = false};
            _searchTimer.Elapsed += (s, ex) =>
            {
                addTrackBtn.Enabled = false;
                Invoke(new MethodInvoker(delegate { searchListBox.ClearSelected(); }));
                var results = _web.Search(searchInput.Text);
                if (results.Tracks == null) return;

                // Check if artist names can be hidden in results (if all are the same)
                var hideArtists = true;
                var tmpName = "";
                foreach (var track in results.Tracks.Items)
                {
                    var name = track.Artists[0].Name;
                    if (tmpName == "") tmpName = name;
                    else if (name != tmpName)
                    {
                        hideArtists = false;
                        break;
                    }
                }

                // Add tracks to the result list
                Invoke(new MethodInvoker(delegate
                {
                    searchListBox.Items.Clear();

                    foreach (FullTrack track in results.Tracks.Items)
                    {
                        searchListBox.Items.Add(new TrackResult(track) {HideArtist = hideArtists});
                    }
                }));
            };
            _searchTimer.Start();
        }

        private void searchListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Enable add-button if item selected
            addTrackBtn.Enabled = searchListBox.SelectedIndex >= 0;
        }

        private void addTrackBtn_Click(object sender, EventArgs e)
        {
            // Add track to the database
            var result = (TrackResult) searchListBox.SelectedItem;
            _playlist.AddTrack(new Track(result.ID, result.Artists[0], result.Name, result.ImageURL, result.Votes));

            // Remove result from the list
            searchListBox.Items.RemoveAt(searchListBox.SelectedIndex);
        }

        #endregion

        #region Database stuff

        private void InitPlaylist()
        {
            RefreshPlaylist();

            new Thread(() =>
            {
//                var playlistChanges = DB.R.Table("playlistTrack").Filter(DB.R.HashMap("playlist", _playlist.ID)).Changes().RunCursor<Playlist>(DB.Connection);
                var voteChanges = DB.R.Table("vote").Changes().RunCursor<Vote>(DB.Connection);
                // TODO: Only changes on current playlist

//                foreach (var change in playlistChanges)
//                {
//                    // TODO: Only update changed
//                    Debug.WriteLine("a");
//                    RefreshPlaylist();
//                }

                foreach (var change in voteChanges)
                {
                    // TODO: Only update changed
                    Debug.WriteLine("b");
                    RefreshPlaylist();
                }
            }) {IsBackground = true}.Start();
        }

        private void RefreshPlaylist() => this.InvokeIfRequired(() => dataGridView1.DataSource = _playlist.GetTracks());
        private void refreshBtn_Click(object sender, EventArgs e) => RefreshPlaylist();

        public bool PlayNextTrack()
        {
            var tracks = _playlist.GetTracks();

            var success = true;
            if (tracks.Length > 0)
            {
                // Find track with most votes
                var track = tracks.MaxBy(t => t.Votes);

                // Remove track from playlist
                track.Delete(_playlist.ID);
                _client.Play(_playlist.ID);
                RefreshPlaylist();
            }
            else success = false;

            UpdateTrackView();
            return success;
        }

        #endregion


        // TEMP:
        private void button1_Click(object sender, EventArgs e)
        {
            // Upvote
            var tracks = _playlist.GetTracks();
            if (tracks.Length > 0)
            {
                tracks[0].Vote(UserID, _playlist.ID);
            }
        }
    }
}