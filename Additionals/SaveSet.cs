        private void XXXX(Set set)
        {
            if (set?.Grid?.Cells != default)
            {
                set.Grid.Cells.Clear();

                var group = new Group();
                set.Grid.Cells.Add(group);

                var gridDamen = new Grid
                {
                    Name = "Damen",
                };

                group.Grids.Add(gridDamen);

                gridDamen.Cells.Add(new Player() { Name = "Sonstiges", Row = 0, Column = 0, Key = KeyCode.Vc7 });
                gridDamen.Cells.Add(new Player() { Name = "Sonstiges", Row = 0, Column = 1, Key = KeyCode.Vc8 });
                gridDamen.Cells.Add(new Player() { Name = "Warm-Up", Row = 0, Column = 2, Key = KeyCode.Vc9, Path = @"C:\Users\mgr\SportDJ\Beginn\Start - Sky News Theme.mp3" });

                gridDamen.Cells.Add(new Player() { Name = "Gast", Row = 1, Column = 0, Key = KeyCode.Vc4, Path = @"C:\Users\mgr\SportDJ\Beginn\Start - Damen\Start - Intro Damen 2017 - Gast.mp3" });
                gridDamen.Cells.Add(new Player() { Name = "Heim", Row = 1, Column = 1, Key = KeyCode.Vc5, Path = @"C:\Users\mgr\SportDJ\Beginn\Start - Damen\Start - Intro Damen 2021 - Heim.mp3" });
                gridDamen.Cells.Add(new Player() { Name = "Warten", Row = 1, Column = 2, Key = KeyCode.Vc6, Path = @"C:\Users\mgr\SportDJ\Start_Damen.m3u" });

                gridDamen.Cells.Add(new Player() { Name = "Einlauf", Row = 2, Column = 0, Key = KeyCode.Vc1, Path = @"C:\Users\mgr\SportDJ\Beginn\Start - Damen\Start - 300 Violin Orchestra.mp3" });
                gridDamen.Cells.Add(new Player() { Name = "Start", Row = 2, Column = 1, Key = KeyCode.Vc2, Path = @"C:\Users\mgr\SportDJ\Beginn\Start - Damen\Start - Ready To Rumble.mp3" });
                gridDamen.Cells.Add(new Player() { Name = "Warten", Row = 2, Column = 2, Key = KeyCode.Vc3, Path = @"C:\Users\mgr\SportDJ\Beginn\Effekt - Are You Ready.mp3", OnForeground = true });

                var gridHerren = new Grid
                {
                    Name = "Herren",
                };

                group.Grids.Add(gridHerren);

                gridHerren.Cells.Add(new Player() { Name = "Sonstiges", Row = 0, Column = 0, Key = KeyCode.Vc7 });
                gridHerren.Cells.Add(new Player() { Name = "Sonstiges", Row = 0, Column = 1, Key = KeyCode.Vc8 });
                gridHerren.Cells.Add(new Player() { Name = "Warm-Up", Row = 0, Column = 2, Key = KeyCode.Vc9, Path = @"C:\Users\mgr\SportDJ\Beginn\Start - Herren\Start - Home With a Vengeance.mp3" });

                gridHerren.Cells.Add(new Player() { Name = "Gast", Row = 1, Column = 0, Key = KeyCode.Vc4, Path = @"C:\Users\mgr\SportDJ\Beginn\Start - Herren\Start - Intro Herren 2017 - Gast.mp3" });
                gridHerren.Cells.Add(new Player() { Name = "Heim", Row = 1, Column = 1, Key = KeyCode.Vc5, Path = @"C:\Users\mgr\SportDJ\Beginn\Start - Herren\Start - Intro Herren 2021 - Heim.mp3" });
                gridHerren.Cells.Add(new Player() { Name = "Warten", Row = 1, Column = 2, Key = KeyCode.Vc6, Path = @"C:\Users\mgr\SportDJ\Start_Herren.m3u" });

                gridHerren.Cells.Add(new Player() { Name = "Einlauf", Row = 2, Column = 0, Key = KeyCode.Vc1, Path = @"C:\Users\mgr\SportDJ\Beginn\Start - Herren\Start - Run Boy Run - Kurz.mp3" });
                gridHerren.Cells.Add(new Player() { Name = "Start", Row = 2, Column = 1, Key = KeyCode.Vc2, Path = @"C:\Users\mgr\SportDJ\Beginn\Start - Herren\Start - Get Ready.mp3" });
                gridHerren.Cells.Add(new Player() { Name = "Warten", Row = 2, Column = 2, Key = KeyCode.Vc3, Path = @"C:\Users\mgr\SportDJ\Beginn\Effekt - Are You Ready.mp3", OnForeground = true });

                var gridSpiel = new Grid
                {
                    Name = "Spiel",
                };

                group.Grids.Add(gridSpiel);

                gridSpiel.Cells.Add(new Player() { Name = "TO Normal", Row = 0, Column = 0, Key = KeyCode.VcNumPadAdd, Path = @"C:\Users\mgr\SportDJ\Spiel_TO-Normal.m3u", SavePositions = true });
                gridSpiel.Cells.Add(new Player() { Name = "TO Knapp", Row = 0, Column = 1, Key = KeyCode.VcNumPadSubtract, Path = @"C:\Users\mgr\SportDJ\Spiel_TO-Knapp.m3u", SavePositions = true });
                gridSpiel.Cells.Add(new Player() { Name = "Sonstiges", Row = 0, Column = 2, Key = KeyCode.Vc7 });

                var groupTor = new Group { Column = 0, Row = 1 };

                gridSpiel.Cells.Add(groupTor);

                var gridTorDamen = new Grid
                {
                    Name = "Tor Damen",
                };

                groupTor.Grids.Add(gridTorDamen);

                gridTorDamen.Cells.Add(new Player() { Name = "Tor", Key = KeyCode.Vc7, Path = @"C:\Users\mgr\SportDJ\Spiel_Tor\Tor - The Concert.mp3" });

                var gridTorHerren = new Grid
                {
                    Name = "Tor Herren",
                };

                groupTor.Grids.Add(gridTorHerren);

                gridTorHerren.Cells.Add(new Player() { Name = "Tor", Key = KeyCode.Vc7, Path = @"C:\Users\mgr\SportDJ\Spiel_Tor\Tor - Zombie Nation.mp3" });

                gridSpiel.Cells.Add(new Player() { Name = "Weiter", Row = 1, Column = 1, Key = KeyCode.Vc8, Path = @"C:\Users\mgr\SportDJ\Spiel_Tor\Effekt - 3 Points.mp3" });
                gridSpiel.Cells.Add(new Player() { Name = "Ende", Row = 1, Column = 2, Key = KeyCode.Vc9, Path = @"C:\Users\mgr\SportDJ\Besonderes\Sirene\Sirene - Basketball Buzzer.mp3", OnForeground = true });

                gridSpiel.Cells.Add(new Player() { Name = "Anheizen", Row = 2, Column = 0, Key = KeyCode.Vc4, Path = @"C:\Users\mgr\SportDJ\Spiel_Anheizen.m3u", SavePositions = true });
                gridSpiel.Cells.Add(new Player() { Name = "Gegner", Row = 2, Column = 1, Key = KeyCode.Vc5, Path = @"C:\Users\mgr\SportDJ\Spiel_Gegner.m3u", SavePositions = true });
                gridSpiel.Cells.Add(new Player() { Name = "Team", Row = 2, Column = 2, Key = KeyCode.Vc6, Path = @"C:\Users\mgr\SportDJ\Spiel_Team.m3u", SavePositions = true, OnForeground = true });

                gridSpiel.Cells.Add(new Player() { Name = "Attacke", Row = 3, Column = 0, Key = KeyCode.Vc1, Path = @"C:\Users\mgr\SportDJ\Spiel_Angriff.m3u", SavePositions = true });
                gridSpiel.Cells.Add(new Player() { Name = "Klatschen Kurz", Row = 3, Column = 1, Key = KeyCode.Vc2, Path = @"C:\Users\mgr\SportDJ\Spiel_Klatschen-Kurz.m3u", SavePositions = true });
                gridSpiel.Cells.Add(new Player() { Name = "Klatschen Lang", Row = 3, Column = 2, Key = KeyCode.Vc3, Path = @"C:\Users\mgr\SportDJ\Spiel_Klatschen-Lang.m3u", SavePositions = true });

                var gridEnde = new Grid
                {
                    Name = "Ende",
                };

                group.Grids.Add(gridEnde);

                gridEnde.Cells.Add(new Player() { Name = "Gewonnen", Row = 0, Column = 0, Key = KeyCode.Vc7, Path = @"C:\Users\mgr\SportDJ\Ende_Gewonnen.m3u" });
                gridEnde.Cells.Add(new Player() { Name = "Unentschieden", Row = 0, Column = 1, Key = KeyCode.Vc8, Path = @"C:\Users\mgr\SportDJ\Ende\Maxine Nightingale - Right Back Where We Started From.mp3" });
                gridEnde.Cells.Add(new Player() { Name = "Verloren", Row = 0, Column = 2, Key = KeyCode.Vc9, Path = @"C:\Users\mgr\SportDJ\Ende_Verloren.m3u" });

                gridEnde.Cells.Add(new Player() { Name = "Sonstiges", Row = 1, Column = 0, Key = KeyCode.Vc1 });
                gridEnde.Cells.Add(new Player() { Name = "Sonstiges", Row = 1, Column = 1, Key = KeyCode.Vc2 });
                gridEnde.Cells.Add(new Player() { Name = "Abschied", Row = 1, Column = 2, Key = KeyCode.Vc3, Path = @"C:\Users\mgr\SportDJ\Ende\Wayne Newton - Danke Schoen.mp3" });

                set.Options.KeyTalkVolume = KeyCode.VcSpace;
                set.Options.KeyPauseOutput = KeyCode.VcEnter;

                set.Options.FadeOnLowerMilliseconds = 50;
                set.Options.FadeOnPauseMilliseconds = 500;
                set.Options.FadeOnMixInMilliseconds = 500;
                set.Options.FadeOnMixOutMilliseconds = 500;

                set.Options.SelectNextSeconds = 5;

                set.Outputs.Single(o => o.Type == OutputType.Live).DeviceName = "Lautsprecher (Realtek(R) Audio)";
                //set.Outputs.Single(o => o.Type == OutputType.Live).DeviceName = "Kopfhoerer (Realtek(R) Audio)";
            }
        }