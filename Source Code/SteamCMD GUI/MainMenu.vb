﻿Imports System.Globalization
Imports System.Threading
Imports System.IO
Imports System.Net
Imports System.Xml
Imports System.Text

Module Module1
    Public SteamAppID, GoldSrcMod As String
    ' Run Server
    Public GameMod, ServerName, ServerMap, NetworkType, MaxPlayers, UDPPort, DebugMode, SourceTV, ConsoleMode, InsecureMode, NoBots, DevMode, AdditionalCommands, Parameters As String
    ' Strings
    Public CantFindSteamCMDString As String = "Can't find the file ""steamcmd.exe""!"
    Public GameDictionary As Dictionary(Of String, String) = New Dictionary(Of String, String)
End Module


Public Class MainMenu
    Dim WithEvents WC As New WebClient

    Dim LocalHost As String = Dns.GetHostName
    Dim IPs As IPHostEntry = Dns.GetHostEntry(LocalHost)
    Dim PublicIP As String

    Private Declare Function GetInputState Lib "user32" () As Int32

    Private Sub Form1_Load() Handles MyBase.Load
        If My.Computer.Network.IsAvailable Then
            PublicIP = WC.DownloadString("http://ipv4.icanhazip.com/")
        Else
            PublicIP = "Network down"
        End If

        Icon = My.Resources.SteamCMDGUI_Icon
        TabMenu.Size = New Size(417, 303)
        ThrSteamCMD = New Thread(AddressOf ThreadTaskSteamCMD)
        ModList.SelectedIndex = 1
        NetworkComboBox.SelectedIndex = 0
        If Not Directory.Exists("Settings") Then
            Directory.CreateDirectory("Settings")
        End If

        If File.Exists("Settings/SteamCMDGames.xml") Then
            LoadGamesList()
        Else
            InitializeDefaultGamesList()
        End If
        GamesList.DataSource = New BindingSource(GameDictionary, Nothing)
        GamesList.DisplayMember = "Value"
        GamesList.ValueMember = "Key"
        GamesList.DataBindings.DefaultDataSourceUpdateMode = DataSourceUpdateMode.OnPropertyChanged
        GamesList.SelectedIndex = 0

        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0F, 13.0F)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
    End Sub

    ' Resize tabs
    Private Sub Tab_Click() Handles UpdateTab.Enter, RunTab.Enter
        If GroupBox1.Visible = False Then
            GroupBox1.Show()
            GroupBox3.Show()
            AboutButton.Show()
            ExitButton.Show()
            DonwloadBar.Show()
            TabMenu.Size = New Size(417, 303)
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0F, 13.0F)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        End If
    End Sub

    ' Update/install server inputs
    Private Sub SteamCMDDownload_Click() Handles SteamCMDDownloadButton.Click
        SteamCMDDownloadButton.Enabled = False
        If My.Computer.FileSystem.FileExists("steamcmd.zip") Then
            Status.Text = "The file has already been downloaded!"
            Status.BackColor = Color.FromArgb(240, 200, 200)
            My.Computer.Audio.PlaySystemSound(
                Media.SystemSounds.Hand)
            SteamCMDDownloadButton.Enabled = True
        Else
            WC.DownloadFileAsync(New Uri("http://media.steampowered.com/installer/steamcmd.zip"), "steamcmd.zip")
            Status.Text = "Downloading..."
            Status.BackColor = Color.FromArgb(240, 240, 240)
        End If
    End Sub

    Private Sub WC_DownloadProgressChanged(ByVal sender As Object, ByVal e As DownloadProgressChangedEventArgs) Handles WC.DownloadProgressChanged
        DonwloadBar.Value = e.ProgressPercentage
        If DonwloadBar.Value = 100 Then
            Status.Text = "The file 'steamcmd.zip' has been downloaded. Please, unzip it."
            Status.BackColor = Color.FromArgb(240, 240, 240)
            DonwloadBar.Value = 0
            My.Computer.Audio.PlaySystemSound(
              Media.SystemSounds.Exclamation)
            SteamCMDDownloadButton.Enabled = True
        End If
    End Sub

    Private Sub ExePath_Browser() Handles ExePath.Click, ExeBrowserButton.Click
        If FolderBrowserDialog1.ShowDialog() = DialogResult.OK Then
            If My.Computer.FileSystem.FileExists(FolderBrowserDialog1.SelectedPath & "\steamcmd.exe") Then
                ExePath.Text = FolderBrowserDialog1.SelectedPath

                Dim CMDConfig As New XmlWriterSettings()
                CMDConfig.Indent = True

                LogMenu.Enabled = True
                Status.Text = "Current path of 'steamcmd.exe' is " & FolderBrowserDialog1.SelectedPath
                Status.BackColor = Color.FromArgb(240, 240, 240)
            Else
                LogMenu.Enabled = False
                Status.Text = CantFindSteamCMDString
                Status.BackColor = Color.FromArgb(240, 200, 200)
                My.Computer.Audio.PlaySystemSound(
                    Media.SystemSounds.Hand)
            End If
        End If
    End Sub

    Private Sub AnonymousCheckBox_CheckedChanged() Handles AnonymousCheckBox.CheckedChanged
        If AnonymousCheckBox.Checked = True Then
            UsernameTextBox.Enabled = False
            PasswordTextBox.Enabled = False
        Else
            UsernameTextBox.Enabled = True
            PasswordTextBox.Enabled = True
        End If
    End Sub

    Private Sub IdHelpButton_Click() Handles IdHelpButton.Click
        Process.Start("https://developer.valvesoftware.com/wiki/Dedicated_Servers_List")
    End Sub

    Private Sub BrowserButton_Browser() Handles BrowserButton.Click, ServerPath.Click
        If FolderBrowserDialog1.ShowDialog() = DialogResult.OK Then
            ServerPath.Text = FolderBrowserDialog1.SelectedPath
            Dim ServerInstallPath As String
            ServerInstallPath = FolderBrowserDialog1.SelectedPath
        End If
        If ServerPath.Text = Nothing Then
            Status.Text = "Please, select a folder for install/update the server."
            Status.BackColor = Color.FromArgb(240, 200, 200)
            My.Computer.Audio.PlaySystemSound(
                Media.SystemSounds.Hand)
        Else
            Status.Text = "The server will be installed/updated in '" & ServerPath.Text & "'"
            Status.BackColor = Color.FromArgb(240, 240, 240)
            UpdateServerButton.Enabled = True
        End If
    End Sub

    Private Sub GamesList_SelectedIndexChanged() Handles GamesList.SelectedIndexChanged, GamesList.EnabledChanged
        If TypeOf (GamesList.SelectedValue) Is KeyValuePair(Of String, String) Then
            SteamAppID = GamesList.SelectedValue.Key
        ElseIf TypeOf (GamesList.SelectedValue) Is Integer Then
            SteamAppID = GamesList.SelectedValue.ToString()
        ElseIf TypeOf (GamesList.SelectedValue) Is String Then
            SteamAppID = GamesList.SelectedValue
        End If

        If Not SteamAppID = 90 Then
            GoldSrcModInput.Hide()
            GoldSrcModLabel.Hide()
            AddCustomGameButton.Show()
        Else
            GoldSrcModInput.Show()
            GoldSrcModLabel.Show()
            AddCustomGameButton.Hide()
        End If

        Status.Text = "Game to install: " & GamesList.Text & " - Steam App ID:" & SteamAppID
        Status.BackColor = Color.FromArgb(240, 240, 240)
    End Sub

    Private Sub ValidateCheckBox_CheckedChanged() Handles ValidateCheckBox.CheckedChanged
        If ValidateCheckBox.Checked = True Then
            Status.Text = "The files will be checked and validated."
        End If
    End Sub

    Private Sub UpdateServerButton_Click() Handles UpdateServerButton.Click
        FolderBrowserDialog1.SelectedPath = ExePath.Text
        If My.Computer.FileSystem.FileExists(FolderBrowserDialog1.SelectedPath & "\steamcmd.exe") Then
            If GamesList.SelectedValue = Nothing Then
                Status.Text = "Steam App ID not defined"
                Status.BackColor = Color.FromArgb(240, 200, 200)
                My.Computer.Audio.PlaySystemSound(
                    Media.SystemSounds.Hand)
            Else
                If UsernameTextBox.Text = Nothing AndAlso AnonymousCheckBox.Checked = False Then
                    Status.Text = "Please, type your Steam name."
                    Status.BackColor = Color.FromArgb(240, 200, 200)
                    My.Computer.Audio.PlaySystemSound(
                        Media.SystemSounds.Hand)
                Else
                    If PasswordTextBox.Text = Nothing AndAlso AnonymousCheckBox.Checked = False Then
                        Status.Text = "Please, type your Steam password. You can install many games as 'anonymous'."
                        Status.BackColor = Color.FromArgb(240, 200, 200)
                        My.Computer.Audio.PlaySystemSound(
                            Media.SystemSounds.Hand)
                    Else
                        If ServerPath.Text = Nothing Then
                            Status.Text = "Please, select the path where you want to install the server."
                            Status.BackColor = Color.FromArgb(240, 200, 200)
                            My.Computer.Audio.PlaySystemSound(
                                Media.SystemSounds.Hand)
                        Else
                            If GoldSrcModInput.Visible = True _
                                AndAlso Not String.IsNullOrEmpty(GoldSrcModInput.Text) Then
                                GoldSrcMod = " +app_set_config 90 mod " & GoldSrcModInput.Text
                            Else
                                Status.Text = "Half-Life mod not defined. Installing a default one."
                                Status.BackColor = Color.FromArgb(240, 200, 200)
                                My.Computer.Audio.PlaySystemSound(
                                    Media.SystemSounds.Hand)
                            End If
                            Status.Text = "Installing/Updating..."
                            Status.BackColor = Color.FromArgb(240, 240, 240)

                            ThrSteamCMD.Start()
                        End If
                    End If
                End If
            End If
        Else
            Status.Text = CantFindSteamCMDString
            Status.BackColor = Color.FromArgb(240, 200, 200)
            My.Computer.Audio.PlaySystemSound(
                Media.SystemSounds.Hand)
        End If
    End Sub

    Private ThrSteamCMD As Thread
    Private WithEvents p As Process

    Private Sub ThreadTaskSteamCMD()
        Control.CheckForIllegalCrossThreadCalls = False
        p = New Process
        With p.StartInfo
            .FileName = ExePath.Text & "\steamcmd.exe"
            .UseShellExecute = False
            .CreateNoWindow = True
            .RedirectStandardOutput = True
            .RedirectStandardInput = True
            .RedirectStandardError = True
            .Arguments = "SteamCmd +login " & If(AnonymousCheckBox.Checked, "anonymous", UsernameTextBox.Text & " " & PasswordTextBox.Text) & " +force_install_dir " & """" & ServerPath.Text & """" & GoldSrcMod & " +app_update " & GamesList.SelectedValue & If((ValidateCheckBox.Checked), " validate", "")
        End With

        p.Start()
    End Sub

    'Run server inputs
    Private Sub SrcdsExePath_Browser() Handles SrcdsExePathTextBox.Click, SrcdsExeBrowserButton.Click
        If FolderBrowserDialog1.ShowDialog() = DialogResult.OK Then
            If My.Computer.FileSystem.FileExists(FolderBrowserDialog1.SelectedPath & "\srcds.exe") Then
                SrcdsExePathTextBox.Text = FolderBrowserDialog1.SelectedPath
                MapList.Enabled = True
                Status.Text = "Current path of 'srcds.exe' is " & FolderBrowserDialog1.SelectedPath
                Status.BackColor = Color.FromArgb(240, 240, 240)
                SrcdsExePathOpen.Enabled = True
                CFGMenu.Enabled = True
                CommonFilesMenu.Enabled = True
                SMMenu.Enabled = True
                RunServerButton.Enabled = True
            Else
                SrcdsExePathOpen.Enabled = False
                MapList.Enabled = False
                CFGMenu.Enabled = False
                CommonFilesMenu.Enabled = False
                SMMenu.Enabled = False
                RunServerButton.Enabled = False
                Status.Text = "Can't find the file 'srcds.exe'!"
                Status.BackColor = Color.FromArgb(240, 200, 200)
                My.Computer.Audio.PlaySystemSound(
                    Media.SystemSounds.Hand)
            End If
        End If
    End Sub

    Private Sub SrcdsExePathOpen_Click() Handles SrcdsExePathOpen.Click
        Process.Start("explorer.exe", SrcdsExePathTextBox.Text)
    End Sub

    Private Sub ModList_SelectedIndex() Handles ModList.SelectedIndexChanged, ModList.EnabledChanged
        If ModList.Text = "Alien Swarm" Then
            GameMod = "alienswarm"
        End If
        If ModList.Text = "Counter-Strike: Global Offensive" Then
            GameMod = "csgo"
        End If
        If ModList.Text = "Counter-Strike: Source" Then
            GameMod = "cstrike"
        End If
        If ModList.Text = "Day of Defeat: Source" Then
            GameMod = "dod"
        End If
        If ModList.Text = "Dota 2" Then
            GameMod = "dota"
        End If
        If ModList.Text = "Garry's Mod" Then
            GameMod = "garrysmod"
        End If
        If ModList.Text = "Half-Life 2: Deathmatch" Then
            GameMod = "hl2mp"
        End If
        If ModList.Text = "Left 4 Dead" Then
            GameMod = "left4dead"
        End If
        If ModList.Text = "Left 4 Dead 2" Then
            GameMod = "left4dead2"
        End If
        If ModList.Text = "Team Fortress 2" Then
            GameMod = "tf"
        End If
        Status.Text = "Game/Mod to run: " & ModList.Text & " - Game parameter: " & GameMod
        Status.BackColor = Color.FromArgb(240, 240, 240)
    End Sub

    Private Sub ModHelpButton_Click() Handles ModHelpButton.Click
        Process.Start("https://developer.valvesoftware.com/wiki/Game_Name_Abbreviations")
    End Sub

    Private Sub CustomModCheckBox_CheckedChanged() Handles CustomModCheckBox.CheckedChanged, CustomModTextBox.TextChanged
        If CustomModCheckBox.Checked = True Then
            ModList.Enabled = False
            CustomModTextBox.Enabled = True
            GameMod = CustomModTextBox.Text
            DebugModeCheckBox.Enabled = False
            SourceTVCheckBox.Enabled = False
            InsecureCheckBox.Enabled = False
            BotsCheckBox.Enabled = False
            DevModeCheckBox.Enabled = False
            Status.Text = "Custom Mod: " & GameMod
            Status.BackColor = Color.FromArgb(240, 240, 240)
        Else
            ModList.Enabled = True
            CustomModTextBox.Enabled = False
            DebugModeCheckBox.Enabled = True
            SourceTVCheckBox.Enabled = True
            InsecureCheckBox.Enabled = True
            BotsCheckBox.Enabled = True
            DevModeCheckBox.Enabled = True
        End If
    End Sub

    Private Sub ServerNameTextBox_TextChanged() Handles ServerNameTextBox.TextChanged
        ServerName = ServerNameTextBox.Text
        Status.Text = "The name of the server will be: " & ServerName
    End Sub

    Private Sub MapList_DropDown() Handles MapList.DropDown
        MapList.Items.Clear()
        Dim mapfolderpath As String
        mapfolderpath = SrcdsExePathTextBox.Text & "\" & GameMod & "\maps"
        If Directory.Exists(mapfolderpath) Then
            For Each MapFile As String In My.Computer.FileSystem.GetFiles _
                (mapfolderpath, FileIO.SearchOption.SearchTopLevelOnly, "*.bsp")
                MapList.Items.Add(Path.GetFileNameWithoutExtension(MapFile))
            Next
        Else
            Status.Text = "The 'map' folder is empty or doesn't exist!"
            Status.BackColor = Color.FromArgb(240, 200, 200)
            My.Computer.Audio.PlaySystemSound(
                Media.SystemSounds.Hand)
        End If
    End Sub

    Private Sub MapList_ChooseMap() Handles MapList.SelectedIndexChanged
        ServerMap = MapList.Text
        Status.Text = "The map of the server will be: " & ServerMap
    End Sub

    Private Sub MaxPlayersTexBox_ValueChanged() Handles MaxPlayersTexBox.TextChanged
        MaxPlayers = MaxPlayersTexBox.Value
        Status.Text = "Max players set to " & MaxPlayers
    End Sub

    Private Sub NetworkComboBox_SelectedIndexChanged() Handles NetworkComboBox.SelectedIndexChanged
        NetworkType = NetworkComboBox.SelectedIndex
        Status.Text = "Cvar sv_lan set to " & NetworkType
    End Sub

    Private Sub UDPPortTexBox_ValueChanged() Handles UDPPortTexBox.TextChanged
        UDPPort = UDPPortTexBox.Value
        Status.Text = "UPD port set to " & UDPPort
    End Sub

    'Command-line Arguments
    Private Sub DebugModeCheckBox_CheckedChanged() Handles DebugModeCheckBox.CheckedChanged
        If DebugModeCheckBox.Checked = True Then
            DebugMode = "-debug "
        Else
            DebugMode = ""
        End If
    End Sub

    Private Sub SourceTVCheckBox_CheckedChanged() Handles SourceTVCheckBox.CheckedChanged
        If SourceTVCheckBox.Checked = True Then
            SourceTV = ""
        Else
            SourceTV = "-nohltv "
        End If
    End Sub

    Private Sub InsecureCheckBox_CheckedChanged() Handles InsecureCheckBox.CheckedChanged
        If InsecureCheckBox.Checked = True Then
            InsecureMode = "-insecure "
        Else
            InsecureMode = ""
        End If
    End Sub

    Private Sub BotsCheckBox_CheckedChanged() Handles BotsCheckBox.CheckedChanged
        If BotsCheckBox.Checked = True Then
            NoBots = "-nobots "
        Else
            NoBots = ""
        End If
    End Sub

    Private Sub DevModeCheckBox_CheckedChanged() Handles DevModeCheckBox.CheckedChanged
        If DevModeCheckBox.Checked = True Then
            DevMode = "-dev "
        Else
            DevMode = ""
        End If
    End Sub

    Private Sub AddButton_Click() Handles AddButton.Click
        CommandLineOptionsWindow.Show()
    End Sub

    Private Sub RunServerButton_Click() Handles RunServerButton.Click
        If My.Computer.FileSystem.FileExists(SrcdsExePathTextBox.Text & "\srcds.exe") Then
            If GameMod = Nothing Then
                Status.Text = "Please, select a game."
                Status.BackColor = Color.FromArgb(240, 200, 200)
                My.Computer.Audio.PlaySystemSound(
                    Media.SystemSounds.Hand)
            Else
                If ServerName = Nothing Then
                    Status.Text = "Please, type a name for the server."
                    Status.BackColor = Color.FromArgb(240, 200, 200)
                    My.Computer.Audio.PlaySystemSound(
                        Media.SystemSounds.Hand)
                Else
                    If ServerMap = Nothing Then
                        Status.Text = "Select the default map."
                        Status.BackColor = Color.FromArgb(240, 200, 200)
                        My.Computer.Audio.PlaySystemSound(
                            Media.SystemSounds.Hand)
                    Else
                        Parameters = DebugMode & SourceTV & ConsoleMode & InsecureMode & NoBots & DevMode
                        Status.Text = "Running server..."
                        Status.BackColor = Color.FromArgb(240, 240, 240)

                        Dim p As New Process
                        With p.StartInfo
                            .FileName = SrcdsExePathTextBox.Text & "\srcds.exe"
                            .UseShellExecute = False
                            .CreateNoWindow = False
                            .Arguments = Parameters & "-game " & GameMod & " -port " & UDPPort & " +hostname " & Chr(34) & ServerName & Chr(34) & " +map " & ServerMap & " +maxplayers " & MaxPlayers & " +sv_lan " & NetworkComboBox.SelectedIndex & " " & AdditionalCommands
                        End With

                        p.Start()
                    End If
                End If
            End If
        Else
            Status.Text = "Can't find the file 'srcds.exe'!"
            Status.BackColor = Color.FromArgb(240, 200, 200)
            My.Computer.Audio.PlaySystemSound(
                Media.SystemSounds.Hand)
        End If
    End Sub

    ' Tools buttons
    Private Sub VDCButton_Click() Handles VDCButton.Click
        Process.Start("https://developer.valvesoftware.com/wiki/SteamCMD")
    End Sub

    Private Sub CheckUpdatesButton_Click() Handles CheckUpdatesButton.Click
        Process.Start("https://github.com/ijre/SteamCMD-GUI/releases/latest")
    End Sub

    Private Sub SMButton_Click() Handles SMButton.Click
        Process.Start("http://www.sourcemod.net")
    End Sub

    Private Sub MMButton_Click() Handles MMButton.Click
        Process.Start("http://www.sourcemm.net")
    End Sub

    Private Sub ESButton_Click() Handles ESButton.Click
        Process.Start("http://addons.eventscripts.com")
    End Sub

    'Private Sub MAPButton_Click() Handles MAPButton.Click
    '    Process.Start("http://mani-admin-plugin.com")
    'End Sub

    Private Sub AboutButton_Click() Handles AboutButton.Click, AboutToolStripMenuItem.Click
        AboutWindow.Show()
    End Sub

    Private Sub ExitButton_Click() Handles ExitButton.Click, ExitMenu.Click
        Close()
    End Sub

    'Menu buttons
    Private Sub SaveMenu_Click() Handles SaveMenu.Click, SaveButton.Click
        SaveFileDialog1.InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Settings")
        SaveFileDialog1.Filter = "Extensible Markup Language (*.xml)|*.xml"
        SaveFileDialog1.FileName = "Config.xml"

        If SrcdsExePathTextBox.Text = Nothing Then
            Status.Text = "Please, select where is located the file 'srcds.exe'."
            Status.BackColor = Color.FromArgb(240, 200, 200)
        Else
            If SaveFileDialog1.ShowDialog() = DialogResult.OK Then
                Dim ConfigFile As String = SaveFileDialog1.FileName
                Dim Config As New XmlWriterSettings()
                Config.Indent = True

                Dim XmlWrt As XmlWriter = XmlWriter.Create(ConfigFile, Config)
                With XmlWrt
                    .WriteStartDocument()
                    .WriteComment("Config used by SteamCMD GUI")
                    .WriteStartElement("Config")

                    .WriteStartElement("SteamCMD-Config")

                    .WriteStartElement("SteamCMD-Path")
                    .WriteString("")
                    .WriteEndElement()

                    .WriteStartElement("Server-Config")

                    .WriteStartElement("Path")
                    .WriteString(SrcdsExePathTextBox.Text)
                    .WriteEndElement()

                    .WriteStartElement("Game")
                    .WriteString(GamesList.SelectedIndex)
                    .WriteEndElement()

                    .WriteStartElement("SaveLogin")
                    .WriteString(SaveLoginDetails.CheckState)
                    .WriteEndElement()

                    .WriteStartElement("LoginAnon")
                    .WriteString(AnonymousCheckBox.CheckState)
                    .WriteEndElement()

                    If SaveLoginDetails.Checked Then
                        .WriteStartElement("LoginUser")
                        .WriteString(UsernameTextBox.Text)
                        .WriteEndElement()

                        .WriteStartElement("LoginPass")
                        .WriteString(PasswordTextBox.Text)
                        .WriteEndElement()
                    End If

                    .WriteStartElement("ValidateFiles")
                    .WriteString(ValidateCheckBox.CheckState)
                    .WriteEndElement()

                    .WriteStartElement("HostName")
                    .WriteString(ServerName)
                    .WriteEndElement()

                    If ModList.Enabled = False Then
                        .WriteStartElement("CustomMod")
                        .WriteString(CustomModTextBox.Text)
                    Else
                        .WriteStartElement("Mod")
                        .WriteString(ModList.Text)
                    End If
                    .WriteEndElement()

                    .WriteStartElement("Map")
                    .WriteString(ServerMap)
                    .WriteEndElement()

                    .WriteStartElement("Network")
                    .WriteString(NetworkType)
                    .WriteEndElement()

                    .WriteStartElement("Players")
                    .WriteString(MaxPlayers)
                    .WriteEndElement()

                    .WriteStartElement("Port")
                    .WriteString(UDPPort)
                    .WriteEndElement()

                    .WriteStartElement("Debug")
                    .WriteValue(DebugModeCheckBox.CheckState)
                    .WriteEndElement()

                    .WriteStartElement("SourceTV")
                    .WriteValue(SourceTVCheckBox.CheckState)
                    .WriteEndElement()

                    .WriteStartElement("Insecure")
                    .WriteValue(InsecureCheckBox.CheckState)
                    .WriteEndElement()

                    .WriteStartElement("NoBots")
                    .WriteValue(BotsCheckBox.CheckState)
                    .WriteEndElement()

                    .WriteStartElement("DevMode")
                    .WriteValue(DevModeCheckBox.CheckState)
                    .WriteEndElement()

                    If Not AdditionalCommands = Nothing Then
                        .WriteStartElement("AdditionalCommands")
                        .WriteString(AdditionalCommands)
                        .WriteEndElement()
                    End If
                    .WriteEndDocument()
                End With
                XmlWrt.Close()
                Status.Text = Path.GetFileName(ConfigFile) & " file saved."
                Status.BackColor = Color.FromArgb(240, 240, 240)
                My.Computer.Audio.PlaySystemSound(
                  Media.SystemSounds.Exclamation)
            End If
        End If
    End Sub

    Private Sub LoadMenu_Click() Handles LoadMenu.Click
        XmlConfigOpenFileDialog.InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Settings")
        XmlConfigOpenFileDialog.FileName = "*.xml"
        XmlConfigOpenFileDialog.Filter = "Extensible Markup Language (*.xml)|*.xml"

        If XmlConfigOpenFileDialog.ShowDialog() = DialogResult.OK Then
            Dim XmlConfig As XmlReader = New XmlTextReader(XmlConfigOpenFileDialog.FileName)
            While XmlConfig.Read()
                Dim type = XmlConfig.NodeType
                If type = XmlNodeType.Element Then
                    If XmlConfig.Name = "SteamCMD" Then
                        ExePath.Text = XmlConfig.ReadInnerXml.ToString()
                    End If
                    If XmlConfig.Name = "Path" Then
                        SrcdsExePathTextBox.Text = XmlConfig.ReadInnerXml.ToString()
                        ServerPath.Text = XmlConfig.ReadInnerXml().ToString()
                        MapList.Enabled = True
                        CFGMenu.Enabled = True
                        CommonFilesMenu.Enabled = True
                        SMMenu.Enabled = True
                        RunServerButton.Enabled = True
                        SrcdsExePathOpen.Enabled = True
                    End If
                    If XmlConfig.Name = "Game" Then
                        GamesList.SelectedIndex = CInt(XmlConfig.ReadInnerXml().ToString())
                    End If
                    If XmlConfig.Name = "SaveLogin" Then
                        SaveLoginDetails.Checked = Val(XmlConfig.ReadInnerXml.Chars(0))
                    End If
                    If XmlConfig.Name = "LoginAnon" Then
                        AnonymousCheckBox.Checked = Val(XmlConfig.ReadInnerXml.Chars(0))
                    End If
                    If XmlConfig.Name = "LoginUser" Then
                        UsernameTextBox.Text = XmlConfig.ReadInnerXml.ToString()
                    End If
                    If XmlConfig.Name = "LoginPass" Then
                        PasswordTextBox.Text = XmlConfig.ReadInnerXml.ToString()
                    End If
                    If XmlConfig.Name = "ValidateFiles" Then
                        ValidateCheckBox.Checked = Val(XmlConfig.ReadInnerXml.Chars(0))
                    End If
                    If XmlConfig.Name = "HostName" Then
                        ServerNameTextBox.Text = XmlConfig.ReadInnerXml.ToString()
                    End If
                    If XmlConfig.Name = "Mod" Then
                        ModList.Text = XmlConfig.ReadInnerXml.ToString()
                        'Define the game with ModList.Text
                        ModList_SelectedIndex()
                    End If
                    If XmlConfig.Name = "CustomMod" Then
                        CustomModTextBox.Text = XmlConfig.ReadInnerXml.ToString
                        CustomModCheckBox.Checked = True
                    End If
                    If XmlConfig.Name = "Map" Then
                        MapList.Enabled = True
                        ServerMap = XmlConfig.ReadInnerXml.ToString()
                        MapList.Text = ServerMap
                    End If
                    If XmlConfig.Name = "Network" Then
                        NetworkComboBox.SelectedIndex = XmlConfig.ReadInnerXml.ToString()
                    End If
                    If XmlConfig.Name = "Players" Then
                        MaxPlayers = XmlConfig.ReadInnerXml.ToString
                        MaxPlayersTexBox.Value = MaxPlayers
                    End If
                    If XmlConfig.Name = "Port" Then
                        UDPPort = XmlConfig.ReadInnerXml.ToString
                        UDPPortTexBox.Value = UDPPort
                    End If
                    If XmlConfig.Name = "Debug" Then
                        DebugModeCheckBox.Checked = Val(XmlConfig.ReadInnerXml.Chars(0))
                    End If
                    If XmlConfig.Name = "SourceTV" Then
                        SourceTVCheckBox.Checked = Val(XmlConfig.ReadInnerXml.Chars(0))
                    End If
                    If XmlConfig.Name = "Insecure" Then
                        InsecureCheckBox.Checked = Val(XmlConfig.ReadInnerXml.Chars(0))
                    End If
                    If XmlConfig.Name = "NoBots" Then
                        BotsCheckBox.Checked = Val(XmlConfig.ReadInnerXml.Chars(0))
                    End If
                    If XmlConfig.Name = "DevMode" Then
                        DevModeCheckBox.Checked = Val(XmlConfig.ReadInnerXml.Chars(0))
                    End If
                    If XmlConfig.Name = "AdditionalCommands" Then
                        AdditionalCommands = XmlConfig.ReadInnerXml.ToString
                    End If
                End If
            End While
            XmlConfig.Close()
            GroupBox1.Show()
            GroupBox3.Show()
            Status.Text = "The config file has been loaded."
            Status.BackColor = Color.FromArgb(240, 240, 240)
        End If
    End Sub

    Private Sub CFGMenu_DropDownOpening() Handles ToolsMenu.Click
        If CFGMenu.Enabled = True Then
            CFGMenu.DropDownItems.Clear()
            CFGMenu.DropDownItems.Add(NewFileToolStripMenuItem)
            CFGMenu.DropDownItems.Add("-")
            Dim cfgfolderpath As String
            cfgfolderpath = SrcdsExePathTextBox.Text & "\" & GameMod & "\cfg"
            If Directory.Exists(cfgfolderpath) = True Then
                'Create new submenu for each cfg file
                For Each CfgFile As String In My.Computer.FileSystem.GetFiles _
                        (cfgfolderpath, FileIO.SearchOption.SearchTopLevelOnly, "*.cfg")
                    Dim text = Path.GetFileNameWithoutExtension(CfgFile)
                    Dim item As ToolStripItem = CFGMenu.DropDownItems.Add(text)
                    item.Tag = CfgFile
                    AddHandler item.Click, AddressOf CfgMenuItems_Click
                    'This works thanks to Hans Passant ^^
                Next
            Else
                Status.Text = "Can't find the CFG folder. New one created."
                Directory.CreateDirectory(cfgfolderpath)
            End If
        Else
            Status.Text = "Can't find the server files!"
            Status.BackColor = Color.FromArgb(240, 200, 200)
            My.Computer.Audio.PlaySystemSound(
                Media.SystemSounds.Hand)
        End If
    End Sub

    Private Sub CfgMenuItems_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim item = CType(sender, ToolStripItem)
        Dim path = CStr(item.Tag)
        Process.Start(path)
    End Sub

    Private Sub NewFile_Click() Handles NewFileToolStripMenuItem.Click
        SaveFileDialog1.InitialDirectory = SrcdsExePathTextBox.Text & "\" & GameMod & "\cfg"
        SaveFileDialog1.Filter = "Configuration files (*.cfg)|*.cfg"
        SaveFileDialog1.FileName = "Config.cfg"
        If SaveFileDialog1.ShowDialog() = DialogResult.OK Then
            File.Create(SaveFileDialog1.FileName).Dispose()
            Process.Start(SaveFileDialog1.FileName)
            Status.Text = "File " & SaveFileDialog1.FileName & " has been saved."
        End If
    End Sub

    Private Sub MenuTxt_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles MotdTxtButton.Click, MapcycleTxtButton.Click, MaplistTxtButton.Click
        Dim TxtFile As ToolStripMenuItem = CType(sender, ToolStripMenuItem)
        Dim MotdPath As String = SrcdsExePathTextBox.Text & "\" & GameMod & "\" & TxtFile.Text & ".txt"
        If File.Exists(MotdPath) Then
            Process.Start(MotdPath)
        Else
            File.Create(MotdPath).Dispose()
            Process.Start(MotdPath)
            Status.Text = TxtFile.Text & " file not found. New one created."
        End If
    End Sub

    Private Sub SMMenu_Click() Handles SMMenu.MouseHover, SMMenu.Click
        If SMMenu.Enabled = True Then
            SMMenu.DropDownItems.Clear()
            Dim SMFilesPath As String
            SMFilesPath = SrcdsExePathTextBox.Text & "\" & GameMod & "\addons\sourcemod\configs"
            If Directory.Exists(SMFilesPath) Then
                'Create new submenu for each cfg and txt file
                For Each SMFile As String In My.Computer.FileSystem.GetFiles _
                        (SMFilesPath, FileIO.SearchOption.SearchTopLevelOnly, "*.cfg", "*.txt", "*.ini")
                    Dim text = Path.GetFileNameWithoutExtension(SMFile)
                    Dim item As ToolStripItem = SMMenu.DropDownItems.Add(text)
                    item.Tag = SMFile
                    AddHandler item.Click, AddressOf SMFileMenuItems_Click
                Next
            Else
                Status.Text = "Seems that SourceMod isn't installed."
                Status.BackColor = Color.FromArgb(240, 200, 200)
                My.Computer.Audio.PlaySystemSound(
                    Media.SystemSounds.Hand)
            End If
        End If
    End Sub

    Private Sub SMFileMenuItems_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim item = CType(sender, ToolStripItem)
        Dim path = CStr(item.Tag)
        Process.Start(path)
    End Sub

    Private Sub LogMenu_Click() Handles LogMenu.MouseHover, LogMenu.Click
        If LogMenu.Enabled = True Then
            LogMenu.DropDownItems.Clear()
            Dim LogFilesPath As String
            LogFilesPath = ExePath.Text & "\logs"
            If Directory.Exists(LogFilesPath) Then
                'Create new submenu for each txt file
                For Each LogFile As String In My.Computer.FileSystem.GetFiles _
                        (LogFilesPath, FileIO.SearchOption.SearchTopLevelOnly, "*.txt")
                    Dim text = Path.GetFileNameWithoutExtension(LogFile)
                    Dim item As ToolStripItem = LogMenu.DropDownItems.Add(text)
                    item.Tag = LogFile
                    AddHandler item.Click, AddressOf LogFileMenuItems_Click
                Next
            End If
        End If
    End Sub

    Private Sub LogFileMenuItems_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim item = CType(sender, ToolStripItem)
        Dim path = CStr(item.Tag)
        Process.Start(path)
    End Sub

    Private Sub AddCustomGameButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles AddCustomGameButton.Click
        Dim Name As String = ""
        Dim ID As String = ""

        Name = InputBox("Custom Game Name")
        ID = InputBox("Custom Game App ID")

        If "" = Name Then
            My.Computer.Audio.PlaySystemSound(
            Media.SystemSounds.Hand)
            MessageBox.Show("Custom Game Name was not entered.", "Add Custom Game Error")
            Return
        End If

        If "" = ID Then
            My.Computer.Audio.PlaySystemSound(
            Media.SystemSounds.Hand)
            MessageBox.Show("Custom Game ID was not entered.", "Add Custom Game Error")
            Return
        End If

        Dim TestInt As Integer = 0
        Integer.TryParse(ID, TestInt)
        If TestInt = 0 Then
            My.Computer.Audio.PlaySystemSound(
            Media.SystemSounds.Hand)
            MessageBox.Show("Custom Game ID was not a number (e.x 444880).", "Add Custom Game Error")
            Return
        End If

        GameDictionary.Add(ID, Name)
        'GamesList.DataSource.ResetBindings(False)
        WriteOutDictionaryAsXml(GameDictionary)
        GamesList.DataSource = New BindingSource(GameDictionary, Nothing)

        GamesList.SelectedIndex = GamesList.FindStringExact(Name)

    End Sub

    Private Sub LoadGamesList()
        Dim XmlDoc As XmlReader = New XmlTextReader("Settings/SteamCMDGames.xml")
        'XmlDoc.ReadToFollowing("Games")
        While XmlDoc.Read()
            Dim type = XmlDoc.NodeType
            If type = XmlNodeType.Element Then
                If XmlDoc.Name = "Game" Then
                    XmlDoc.MoveToAttribute("id")
                    Dim ID As String = XmlDoc.Value
                    XmlDoc.Read() 'move pointer to next node part
                    If XmlDoc.NodeType = XmlNodeType.Text Then
                        Dim Name As String = XmlDoc.Value
                        GameDictionary.Add(ID, Name)
                    End If
                End If
            End If

        End While
        XmlDoc.Close()
    End Sub

    Private Sub InitializeDefaultGamesList()
        GameDictionary.Add("635", "Alien Swarm")
        GameDictionary.Add("740", "Counter-Strike: Global Offensive")
        GameDictionary.Add("232330", "Counter-Strike: Source")
        GameDictionary.Add("232290", "Day of Defeat: Source")
        GameDictionary.Add("570", "Dota 2")
        GameDictionary.Add("4020", "Garry's Mod")
        GameDictionary.Add("90", "Half-Life Dedicated Server")
        GameDictionary.Add("232370", "Half-Life 2: Deathmatch")
        GameDictionary.Add("510", "Left 4 Dead")
        GameDictionary.Add("222860", "Left 4 Dead 2")
        GameDictionary.Add("232250", "Team Fortress 2")
        WriteOutDictionaryAsXml(GameDictionary)
    End Sub

    Private Sub WriteOutDictionaryAsXml(ByVal dict As Dictionary(Of String, String))
        Dim XmlSettings As XmlWriterSettings = New XmlWriterSettings()
        XmlSettings.Indent = True
        Dim XmlWrt As XmlWriter = XmlWriter.Create("Settings/SteamCMDGames.xml", XmlSettings)

        XmlWrt.WriteStartDocument()
        XmlWrt.WriteComment("Custom Games Config used by SteamCMD GUI")
        XmlWrt.WriteComment("This config is loaded automatically.")
        XmlWrt.WriteStartElement("SteamCMD-Games")

        For Each kvp As KeyValuePair(Of String, String) In dict
            XmlWrt.WriteStartElement("Game")
            XmlWrt.WriteAttributeString("id", kvp.Key)
            XmlWrt.WriteString(kvp.Value)
            XmlWrt.WriteEndElement()
        Next
        XmlWrt.WriteEndElement()
        XmlWrt.WriteEndDocument()
        XmlWrt.Close()
    End Sub

    Private Sub IPButton_Click() Handles IPButton.Click
        Clipboard.SetText(PublicIP, TextDataFormat.UnicodeText)
        Status.Text = "Public IP copied"
        Status.BackColor = Color.FromArgb(240, 240, 240)
    End Sub
End Class
