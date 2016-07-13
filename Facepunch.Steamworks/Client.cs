﻿

using System;
using System.Runtime.InteropServices;

namespace Facepunch.Steamworks
{
    public partial class Client : IDisposable
    {
        private uint _hpipe;
        private uint _huser;

        internal Valve.Steamworks.ISteamClient _client;
        internal Valve.Steamworks.ISteamUser _user;
        internal Valve.Steamworks.ISteamFriends _friends;
        internal Valve.Steamworks.ISteamMatchmakingServers _servers;
        internal Valve.Steamworks.ISteamInventory _inventory;

        /// <summary>
        /// Current running program's AppId
        /// </summary>
        public uint AppId;

        /// <summary>
        /// Current user's Username
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// Current user's SteamId
        /// </summary>
        public ulong SteamId;

        public Client( uint appId )
        {
            Valve.Steamworks.SteamAPI.Init( appId );
            _client = Valve.Steamworks.SteamAPI.SteamClient();

            if ( _client.GetIntPtr() == IntPtr.Zero )
            {
                _client = null;
                return;
            }

            //
            // Set up warning hook callback
            //
            SteamAPIWarningMessageHook ptr = OnWarning;
            _client.SetWarningMessageHook( Marshal.GetFunctionPointerForDelegate( ptr ) );

            //
            // Get pipes
            //
            _hpipe = _client.CreateSteamPipe();
            _huser = _client.ConnectToGlobalUser( _hpipe );


            //
            // Get other interfaces
            //
            _friends = _client.GetISteamFriends( _huser, _hpipe, "SteamFriends015" );
            _user = _client.GetISteamUser( _huser, _hpipe, "SteamUser019" );
            _servers = _client.GetISteamMatchmakingServers( _huser, _hpipe, "SteamMatchMakingServers002" );
            _inventory = _client.GetISteamInventory( _huser, _hpipe, "STEAMINVENTORY_INTERFACE_V001" );

            AppId = appId;
            Username = _friends.GetPersonaName();
            SteamId = _user.GetSteamID();
        }

        public void Dispose()
        {
            if ( _client != null )
            {
                if ( _hpipe > 0 )
                {
                    if ( _huser  > 0 )
                        _client.ReleaseUser( _hpipe, _huser );

                    _client.BReleaseSteamPipe( _hpipe );

                    _huser = 0;
                    _hpipe = 0;
                }

                _friends = null;

                _client.BShutdownIfAllPipesClosed();
                _client = null;
            }
        }

        [UnmanagedFunctionPointer( CallingConvention.Cdecl )]
        public delegate void SteamAPIWarningMessageHook( int nSeverity, System.Text.StringBuilder pchDebugText );

        private void OnWarning( int nSeverity, System.Text.StringBuilder text )
        {
            Console.Write( text.ToString() );
        }

        /// <summary>
        /// Should be called at least once every frame
        /// </summary>
        public void Update()
        {
            Valve.Steamworks.SteamAPI.RunCallbacks();
            Voice.Update();
            Inventory.Update();
        }

        public bool Valid
        {
            get { return _client != null; }
        }

        internal Action InstallCallback<T>( int type, Action<T> action )
        {
            var ptr = Marshal.GetFunctionPointerForDelegate( action );
            Valve.Steamworks.SteamAPI.RegisterCallback( ptr, type );

            return () => Valve.Steamworks.SteamAPI.UnregisterCallback( ptr );
        }
    }
}