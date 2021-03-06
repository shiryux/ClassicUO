﻿#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;

using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    internal class PartyGumpAdvanced : Gump
    {
        private readonly Button _createAddButton;
        private readonly Label _createAddLabel;
        private readonly GameBorder _gameBorder;
        private readonly GumpPicTiled _gumpPicTiled;
        private readonly Button _leaveButton;
        private readonly Label _leaveLabel;
        private readonly Texture2D _line;
        private readonly Button _lootMeButton;
        private readonly Label _lootMeLabel;
        private readonly Button _messagePartyButton;
        private readonly Label _messagePartyLabel;
        private readonly List<PartyListEntry> _partyListEntries;
        private readonly ScrollArea _scrollArea;

        public PartyGumpAdvanced() : base(0, 0)
        {
            _partyListEntries = new List<PartyListEntry>();
            _line = new Texture2D(Engine.Batcher.GraphicsDevice, 1, 1);

            _line.SetData(new[]
            {
                Color.White
            });
            X = 100;
            Y = 100;
            CanMove = true;
            AcceptMouseInput = false;
            Add(_gameBorder = new GameBorder(0, 0, 320, 400, 4));

            Add(_gumpPicTiled = new GumpPicTiled(4, 4, 320 - 8, 400 - 8, 0x0A40)
            {
                IsTransparent = true
            });
            Add(_gumpPicTiled);

            _scrollArea = new ScrollArea(20, 60, 295, 190, true)
            {
                AcceptMouseInput = true
            };
            Add(_scrollArea);

            Add(new Label("Bar", true, 1153)
            {
                X = 30, Y = 25
            });

            Add(new Label("Kick", true, 1153)
            {
                X = 60, Y = 25
            });

            Add(new Label("Player", true, 1153)
            {
                X = 100, Y = 25
            });

            Add(new Label("Status", true, 1153)
            {
                X = 250, Y = 25
            });

            //======================================================
            Add(_messagePartyButton = new Button((int) Buttons.Message, 0xFAB, 0xFAC, 0xFAD)
            {
                X = 30, Y = 275, ButtonAction = ButtonAction.Activate, IsVisible = false
            });

            Add(_messagePartyLabel = new Label("Message party", true, 1153)
            {
                X = 70, Y = 275, IsVisible = false
            });

            //======================================================
            Add(_lootMeButton = new Button((int) Buttons.Loot, 0xFA2, 0xFA3, 0xFA4)
            {
                X = 30, Y = 300, ButtonAction = ButtonAction.Activate, IsVisible = false
            });

            Add(_lootMeLabel = new Label("Party CANNOT loot me", true, 1153)
            {
                X = 70, Y = 300, IsVisible = false
            });

            //======================================================
            Add(_leaveButton = new Button((int) Buttons.Leave, 0xFAE, 0xFAF, 0xFB0)
            {
                X = 30, Y = 325, ButtonAction = ButtonAction.Activate, IsVisible = false
            });

            Add(_leaveLabel = new Label("Leave party", true, 1153)
            {
                X = 70, Y = 325, IsVisible = false
            });

            //======================================================
            Add(_createAddButton = new Button((int) Buttons.Add, 0xFA8, 0xFA9, 0xFAA)
            {
                X = 30, Y = 350, ButtonAction = ButtonAction.Activate
            });

            Add(_createAddLabel = new Label("Add party member", true, 1153)
            {
                X = 70, Y = 350
            });
            //======================================================
            World.Party.PartyMemberChanged += OnPartyMemberChanged;
        }

        private void OnPartyMemberChanged(object sender, EventArgs e)
        {
            OnInitialize();
        }

        protected override void OnInitialize()
        {
            _scrollArea.Clear();

            foreach (PartyListEntry entry in _partyListEntries)
            {
                entry.Clear();
                entry.Dispose();
            }

            _partyListEntries.Clear();
            foreach (PartyMember member in World.Party.Members) _partyListEntries.Add(new PartyListEntry(member));
            for (int i = 0; i < _partyListEntries.Count; i++) _scrollArea.Add(_partyListEntries[i]);
        }

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            base.Draw(batcher, position, hue);
            batcher.Draw2D(_line, new Rectangle(position.X + 30, position.Y + 50, 260, 1), ShaderHuesTraslator.GetHueVector(0, false, .5f, false));
            batcher.Draw2D(_line, new Rectangle(position.X + 95, position.Y + 50, 1, 200), ShaderHuesTraslator.GetHueVector(0, false, .5f, false));
            batcher.Draw2D(_line, new Rectangle(position.X + 245, position.Y + 50, 1, 200), ShaderHuesTraslator.GetHueVector(0, false, .5f, false));
            batcher.Draw2D(_line, new Rectangle(position.X + 30, position.Y + 250, 260, 1), ShaderHuesTraslator.GetHueVector(0, false, .5f, false));

            return true;
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (!World.Party.IsInParty)
            {
                //Set gump size if player is NOT in party
                _gameBorder.Height = 320;
                _gumpPicTiled.Height = _gameBorder.Height - 12;
                //Set contents if player is NOT in party
                _createAddButton.Y = 270;
                _createAddLabel.Y = _createAddButton.Y;
                _createAddLabel.Text = "Create a party";
                _leaveButton.IsVisible = false;
                _leaveLabel.IsVisible = false;
                _lootMeButton.IsVisible = false;
                _lootMeLabel.IsVisible = false;
                _messagePartyButton.IsVisible = false;
                _messagePartyLabel.IsVisible = false;
            }
            else
            {
                //Set gump size if player is in party
                _gameBorder.Height = 400;
                _gumpPicTiled.Height = _gameBorder.Height - 12;
                //Set contents if player is in party
                _createAddButton.Y = 350;
                _createAddLabel.Y = _createAddButton.Y;
                _createAddLabel.Text = "Add a member";
                _leaveButton.IsVisible = true;
                _leaveLabel.IsVisible = true;
                _lootMeButton.IsVisible = true;
                _lootMeLabel.IsVisible = true;
                _lootMeLabel.Text = !World.Party.AllowPartyLoot ? "Party CANNOT loot me" : "Party ALLOWED looting me";
                _messagePartyButton.IsVisible = true;
                _messagePartyLabel.IsVisible = true;
            }

            base.Update(totalMS, frameMS);
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.Add:
                    World.Party.TriggerAddPartyMember();

                    break;
                case Buttons.Leave:
                    World.Party.QuitParty();

                    break;
                case Buttons.Loot:
                    World.Party.AllowPartyLoot = !World.Party.AllowPartyLoot ? true : false;

                    break;
                case Buttons.Message:

                    //
                    break;
            }
        }

        private enum Buttons
        {
            Add = 1,
            Leave,
            Loot,
            Message
        }

        public override void Dispose()
        {
            base.Dispose();
            _line.Dispose();
        }
    }

    internal class PartyListEntry : Control
    {
        public readonly Button KickButton;
        public readonly PartyMember Member;
        public readonly Label Name;
        public readonly Button PMButton;
        public readonly GumpPic Status;

        public PartyListEntry(PartyMember member)
        {
            Height = 20;
            Member = member;

            //======================================================
            //Name = new Label(member.Name, true, 1153, font:3);
            //Name.X = 80;
            string name = string.IsNullOrEmpty(member.Name) ? "<Not seen>" : member.Name;

            Name = World.Party.Leader == member.Serial
                       ? new Label(name + "(L)", true, 1161, font: 3)
                       {
                           X = 80
                       }
                       : new Label(name, true, 1153, font: 3)
                       {
                           X = 80
                       };
            Add(Name);

            //======================================================
            if (Member.Mobile != null && Member.Mobile.IsDead)
                Status = new GumpPic(240, 0, 0x29F6, 0);
            else
                Status = new GumpPic(240, 0, 0x29F6, 0x43);
            Add(Status);

            //======================================================
            PMButton = new Button((int) Buttons.GetBar, 0xFAE, 0xFAF, 0xFB0)
            {
                X = 10, ButtonAction = ButtonAction.Activate
            };
            Add(PMButton);

            //======================================================
            KickButton = new Button((int) Buttons.Kick, 0xFB1, 0xFB2, 0xFB3)
            {
                X = 40, ButtonAction = ButtonAction.Activate
            };
            Add(KickButton);
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.Kick:
                    //
                    World.Party.RemovePartyMember(Member.Serial);

                    break;
                case Buttons.GetBar:

                    Engine.UI.GetByLocalSerial<HealthBarGump>(Member.Mobile)?.Dispose();

                    if (Member.Mobile == World.Player)
                        StatusGumpBase.GetStatusGump()?.Dispose();

                    Rectangle rect = FileManager.Gumps.GetTexture(0x0804).Bounds;
                    Engine.UI.Add(new HealthBarGump(Member.Mobile) { X = Mouse.Position.X - (rect.Width >> 1), Y = Mouse.Position.Y - (rect.Height >> 1) });

                    break;
            }
        }

        private enum Buttons
        {
            Kick = 1,
            GetBar
        }
    }
}