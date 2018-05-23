using System;
using System.Collections.Generic;
using System.IO;
using Eto.Forms;
using Eto.Drawing;
using System.Diagnostics;
using System.Reflection;
using PartialityLauncher.Properties;
using System.Net;

namespace PartialityLauncher {
    public partial class MainForm: Form {

        public static Image gameWallpaper;
        public static StackLayout modList;
        public static Button runGameButton;

        public Bitmap patchIcon;
        public Bitmap modIcon;
        public Bitmap standaloneIcon;

        public Label gameNameLabel;
        public MaskedTextBox appidBox;

        public const string HEADERURL = "https://steamcdn-a.opskins.media/steam/apps/{0}/header.jpg";

        public MainForm(string[] args) {
            Title = "Partiality Launcher";
            ClientSize = new Size( 500, 700 );
            this.Resizable = false;

            var openCommand = new Command { MenuText = "Open Game", Shortcut = Application.Instance.CommonModifier | Keys.O };
            openCommand.Executed += (sender, e) => OpenGame();

            var quitCommand = new Command { MenuText = "Quit", Shortcut = Application.Instance.CommonModifier | Keys.Q };
            quitCommand.Executed += (sender, e) => Application.Instance.Quit();

            var aboutCommand = new Command { MenuText = "About..." };
            aboutCommand.Executed += (sender, e) => new AboutDialog().ShowDialog( this );

            var runGameCommand = new Command { };
            runGameCommand.Executed += (sender, e) => PatchGame();

            var clearMetaCommand = new Command();
            clearMetaCommand.Executed += (sender, e) => ClearMetadata();

            var refreshMenuCommand = new Command { Shortcut = Application.Instance.CommonModifier | Keys.R };
            refreshMenuCommand.Executed += (sender, e) => GameManager.SaveAllMetadata();
            refreshMenuCommand.Executed += (sender, e) => FillOutMods();

            var uninstallCommand = new Command();
            uninstallCommand.Executed += (sender, e) => GameManager.Uninstall();

            gameWallpaper = new Bitmap( 125, 125, PixelFormat.Format24bppRgb, new List<int>() );
            runGameButton = new Button { Text = "Apply Mods", Size = new Size( 475, 25 ), Command = runGameCommand, Enabled = false };
            gameNameLabel = new Label { Text = "Game Name", TextAlignment = TextAlignment.Left, Font = new Font( "SystemFont.Bold", 19.8f ) };
            appidBox = new MaskedTextBox { ToolTip = "The APPID of the game", PlaceholderText = "APPID of the game", Size = new Size( 150, 25 ) };

            modIcon = new Bitmap( Resources.modIcon );
            patchIcon = new Bitmap( Resources.patchIcon );
            standaloneIcon = new Bitmap( Resources.standaloneIcon );
            Bitmap bmp = new Bitmap( Resources.partiality_p_2 );
            Icon = new Icon( 1, bmp );

            modList = new StackLayout() {
                Padding = 10,
                BackgroundColor = new Color( 0.8f, 0.8f, 0.8f ),
            };

            Content = new StackLayout {
                Padding = 10,
                Spacing = 10,
                Items =
                {
                    new StackLayout{Orientation = Orientation.Horizontal, Spacing = 10,
                        Items = { gameWallpaper ,
                            new StackLayout {
                                Items = {
                                    gameNameLabel,
                                    appidBox
                                }
                            },
                            new StackLayout { Spacing = 9,
                                Items = {
                                    new Button { Text = "Refresh Mod List", Command = refreshMenuCommand},
                                    new Button { Text = "Clear Mod Metadata", Command = clearMetaCommand },
                                    new Button { Text = "Uninstall Partiality", Command = uninstallCommand}
                                }
                            }
                        }
                    },
                    new Scrollable{ Content = modList, Size = new Size(475, 500), ExpandContentWidth = true, ExpandContentHeight = true },
                    runGameButton
                }
            };

            // create menu
            Menu = new MenuBar {
                Items =
                {
					// File submenu
					new ButtonMenuItem { Text = "&File", Items = { openCommand } },
					//new ButtonMenuItem { Text = "&Edit", Items = { /* commands/items */ } },
					// new ButtonMenuItem { Text = "&View", Items = { /* commands/items */ } },
				},
                ApplicationItems =
                {
					// application (OS X) or file menu (others)
					new ButtonMenuItem { Text = "&Preferences...", Items = {  } },
                },
                QuitItem = quitCommand,
                AboutItem = aboutCommand
            };

            UITimer timer = new UITimer();
            timer.Interval = 1 / 60f;
            timer.Elapsed += (sender, e) => this.DrawUpdate();
            timer.Start();

            GameManager.LoadLastGame( this );
        }

        public void DrawUpdate() {
            this.Invalidate();
        }

        public void OpenGame() {
            FileDialog exeDialogue = new OpenFileDialog();
            exeDialogue.ShowDialog( this.Parent );

            if( exeDialogue.FileName == string.Empty || !GameManager.IsValidGamePath( exeDialogue.FileName ) ) {
                return;
            }

            GameManager.Reset();

            GameManager.exePath = exeDialogue.FileName;
            GameManager.GetAppID( this );

            appidBox.Text = GameManager.appID;
            gameNameLabel.Text = Path.GetFileNameWithoutExtension( GameManager.exePath );

            FillOutMods();
            runGameButton.Enabled = true;
        }

        public void FillOutMods() {

            modList.Items.Clear();
            GameManager.LoadModMetas();

            int i = 0;
            foreach( ModMetadata md in GameManager.modMetas ) {
                Bitmap icon = md.isStandalone ? standaloneIcon : ( md.isPatch ? patchIcon : modIcon );
                Icon resized = icon.WithSize( 15, 15 );

                int tmpIndex = i;

                CheckBox cb = new CheckBox() { Checked = md.isEnabled };
                cb.CheckedChanged += (a, b) => SetModEnabled( cb, tmpIndex );

                StackLayout sl = new StackLayout {
                    BackgroundColor = md.isStandalone ? new Color( 0.7f, 0.7f, 0.7f ) : new Color( 0, 0, 0, 0 ),
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Padding = 2,
                    ToolTip = md.modPath,
                    Items = { resized, cb, Path.GetFileName( md.modPath ) },
                };
                modList.Items.Add( sl );
                i++;
            }
        }
        public void SetModEnabled(CheckBox box, int index) {

            ModIncompatibilityWarning warningLevel = GameManager.CheckForModIncompatibilities( index );
            ModMetadata md = GameManager.modMetas[index];
            string fileName = Path.GetFileName( md.modPath );

            if( md.isStandalone ) {
                int i = 0;

                foreach( ModMetadata oMD in GameManager.modMetas ) {
                    if( oMD != md && oMD.isStandalone ) {
                        oMD.isStandalone = false;
                        ( ( modList.Items[i].Control as StackLayout ).Items[1].Control as CheckBox ).Checked = false;
                    }
                    i++;
                }
            }

            if( md.isEnabled == false ) {
                if( warningLevel.warningLevel == 1 ) {
                    MessageBox.Show( string.Format( "The mod {0} might not work with the following mods:{1}{2}", fileName, Environment.NewLine, warningLevel.sameClassMods ) );
                } else if( warningLevel.warningLevel == 2 ) {
                    string msg = string.Format( "The mod {0} is incompatible with the following mods:{1}{2}{1}Don't expect them both to work together, but you can still launch the game.", fileName, Environment.NewLine, warningLevel.sameFunctionMods );
                    if( warningLevel.sameClassMods.Length > 0 ) {
                        msg += string.Format( "It also might not work with these mods:{0}{1}", Environment.NewLine, warningLevel.sameClassMods );
                    }
                    MessageBox.Show( msg );
                }
            }

            md.isEnabled = !md.isEnabled;
        }

        public void ClearMetadata() {
            DialogResult dr = MessageBox.Show( "You're about to delete all mod hashes and metadata! This will reset all your mod settings and disable all mods. Are you sure you want to proceed?", MessageBoxButtons.YesNo );

            if( dr == DialogResult.Yes ) {
                GameManager.ClearMetas();
                FillOutMods();
            }
        }

        public void PatchGame() {
            GameManager.SaveAllMetadata();
            GameManager.appID = appidBox.Text;


            DebugLogger.Log( "Run Game" );
            GameManager.PatchGame();

            if( int.TryParse( appidBox.Text, out int id ) == false ) {
                MessageBox.Show( this, "Mods applied! No/Incorrect APPID was entered, so the game can't be automatically launched. You can now launch the game yourself, as you normally would", "Mod Results", MessageBoxType.Information );
            } else {
                DialogResult result = MessageBox.Show( this, "Mods applied! Would you like to launch the game?", "Mod results", MessageBoxButtons.YesNo );

                if( result == DialogResult.Yes ) {
                    foreach( Control c in Children )
                        c.Enabled = false;

                    GameManager.StartGame();

                    foreach( Control c in Children )
                        c.Enabled = true;
                }
            }

        }
    }
}
