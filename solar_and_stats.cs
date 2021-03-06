String group_name = "";
String SCRIPT_PREFIX = "PowerCTL";
List <TextPanel> text_panels;
Dictionary<string, Airlock> airlocks = new Dictionary<string, Airlock>();
//Dictionary<IMyGasTank>

TimeSpan UPDATE_INTERVAL = TimeSpan.FromSeconds(5.0);
DateTime prev_time = DateTime.UtcNow;
float prev_hydrogen;
string current_hydrogen_time = "***";

string DBG = "";

public class Airlock
{
    public string name = null;
    public IMyAirVent vent_a = null;
    public IMyAirVent vent_b = null;
    public List<IMyDoor> doors = new List<IMyDoor>();
    public List<TextPanel> panels = new List<TextPanel>();
    public VentStatus vent_status_a = VentStatus.Depressurized;
    public VentStatus vent_status_b = VentStatus.Depressurized;
}

public class TextPanel
{
    public string name = null;
    public IMyTextSurface panel;
    public int include_battery;
	public bool b = false;
    public RectangleF viewport;
}


bool scanning = true;
int stop_scanning = 0;
float prev_power = 0;
int panels = 8;
IMyMotorStator rotor = null;
IMyGyro gyro = null;
IMySolarPanel panel = null;

string dbg="";

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Once | UpdateFrequency.Update100;
    
    Reset();
    // The constructor, called only once every session and
    // always before any other method is called. Use it to
    // initialize your script. 
    //     
    // The constructor is optional and can be removed if not
    // needed.
    // 
    // It's recommended to set RuntimeInfo.UpdateFrequency 
    // here, which will allow your script to run itself without a 
    // timer block.
}

public void FindTextPanels()
{
    List<IMyTextPanel> blocks = new List<IMyTextPanel>();
    GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(blocks);

    foreach(IMyTextPanel text_panel in blocks)
    {
        String[] line_vals = text_panel.CustomData.Split('\n')[0].Split(':');
        if (line_vals.Length>1 && line_vals[1].Equals(group_name) && line_vals[0].Equals(SCRIPT_PREFIX))
        {
            DBG = DBG + '\n' + text_panel.CustomName + " - " +  line_vals[1] + " - " + line_vals[2];
            DBG = DBG + " - " + Convert.ToInt32(line_vals[3]);

            AddTextPanel(line_vals,text_panel);
        }
    }

    List<IMyTerminalBlock> blocks_cp = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyCockpit>(blocks_cp);
    HandleTextSurfaceBlocks(blocks_cp);
    GridTerminalSystem.GetBlocksOfType<IMyButtonPanel>(blocks_cp);
    HandleTextSurfaceBlocks(blocks_cp);
}

public void HandleTextSurfaceBlocks(List<IMyTerminalBlock> blocks)
{
    foreach(IMyTerminalBlock button_panel in blocks)
    {
        String[] lines = button_panel.CustomData.Split('\n');
        foreach(String line in lines)
        {
            String[] line_vals = line.Split(':');
            if (line_vals.Length>1 && line_vals[1].Equals(group_name) && line_vals[0].Equals(SCRIPT_PREFIX))
            {
                Echo(button_panel.CustomName);
                IMyTextSurfaceProvider text_panel =  button_panel as IMyTextSurfaceProvider;
                DBG = DBG + '\n' + button_panel.CustomName + " - " +  line_vals[1] + " - " + line_vals[2];
                DBG = DBG + " - " +line_vals[3];
    
                AddTextPanel(line_vals,text_panel.GetSurface(Convert.ToInt32(line_vals[2])));
            }
        }
    }
}

public void AddTextPanel(String[] line_vals,IMyTextSurface text_panel)
{
    bool new_airlock;
    Airlock airlock;
    TextPanel panel = new TextPanel();
    panel.panel = text_panel;
    panel.include_battery = Convert.ToInt32(line_vals[3]);

    Echo(text_panel.TextureSize.ToString());
    Echo(text_panel.SurfaceSize.ToString());


    panel.viewport = new RectangleF(
        (text_panel.TextureSize - text_panel.SurfaceSize) / 2f,
        text_panel.SurfaceSize
    );
    if((panel.include_battery & 0x10) != 0)
    {
        DBG = DBG + " - " + line_vals[4]; 
        new_airlock = !airlocks.TryGetValue (line_vals[4], out airlock);
        if (new_airlock)
        {
            airlock = new Airlock();
            airlocks.Add(line_vals[4], airlock);
        }
        panel.b = line_vals[5].Equals("B");
        
        airlock.panels.Add(panel);
    }
    else
    {
        text_panels.Add(panel);
    }
}

public void FindGyros()
{
    List<IMyGyro> blocks = new List<IMyGyro>();
    GridTerminalSystem.GetBlocksOfType<IMyGyro>(blocks);

    foreach(IMyGyro found_gyro in blocks)
    {
        String[] line_vals = found_gyro.CustomData.Split('\n')[0].Split(':');
        if (line_vals.Length>1 && line_vals[1].Equals(group_name) && line_vals[0].Equals(SCRIPT_PREFIX))
        {
           
           DBG = DBG + '\n' + found_gyro.CustomName + " - " +  line_vals[1];

           gyro =  found_gyro;
        }
    }
}

public void FindRotors()
{
    List<IMyMotorStator> blocks = new List<IMyMotorStator>();
    GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(blocks);

    foreach(IMyMotorStator found_rotor in blocks)
    {
        String[] line_vals = found_rotor.CustomData.Split('\n')[0].Split(':');
        if (line_vals.Length>1 && line_vals[1].Equals(group_name) && line_vals[0].Equals(SCRIPT_PREFIX))
        {
           
           DBG = DBG + '\n' + found_rotor.CustomName + " - " +  line_vals[1];

           rotor =  found_rotor;
        }
    }
}

public void FindPanels()
{
    List<IMySolarPanel> blocks = new List<IMySolarPanel>();
    GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(blocks);

    foreach(IMySolarPanel found_panel in blocks)
    {
        String[] line_vals = found_panel.CustomData.Split('\n')[0].Split(':');
        if (line_vals.Length>1 && line_vals[1].Equals(group_name) && line_vals[0].Equals(SCRIPT_PREFIX))
        {
           
           DBG = DBG + '\n' + found_panel.CustomName + " - " +  line_vals[1];

           panel =  found_panel;
        }
    }
}

public void FindVents()
{
    List<IMyAirVent> blocks = new List<IMyAirVent>();
    GridTerminalSystem.GetBlocksOfType<IMyAirVent>(blocks);

    foreach(IMyAirVent vent in blocks)
    {
        foreach(String line in vent.CustomData.Split('\n'))
        {
            String[] line_vals = line.Split(':');
            if (line_vals.Length>=3 && line_vals[1].Equals(group_name) && line_vals[0].Equals(SCRIPT_PREFIX))
            {
               bool new_airlock;
               Airlock airlock;
               DBG = DBG + '\n' + vent.CustomName + " - " +  line_vals[1] + " - " + line_vals[2];

               new_airlock = !airlocks.TryGetValue (line_vals[2], out airlock);
               if (new_airlock)
               {
                    airlock = new Airlock();
                    airlock.name = line_vals[1];          
                    airlocks.Add(line_vals[2], airlock);
               }
               if (line_vals[3].Equals("A"))
               {
                    airlock.vent_a = vent;
               }
               else
               {
                    airlock.vent_b = vent;
               }
            }
        }
    }
}
public void FindDoors()
{
    List<IMyDoor> blocks = new List<IMyDoor>();
    GridTerminalSystem.GetBlocksOfType<IMyDoor>(blocks);

    foreach(IMyDoor door in blocks)
    {
        String[] line_vals = door.CustomData.Split('\n')[0].Split(':');
        if (line_vals.Length>1 && line_vals[1].Equals(group_name) && line_vals[0].Equals(SCRIPT_PREFIX))
        {
           bool new_airlock;
           Airlock airlock;
           DBG = DBG + '\n' + door.CustomName + " - " +  line_vals[1] + " - " + line_vals[2];

           new_airlock = !airlocks.TryGetValue (line_vals[2], out airlock);
           if (new_airlock)
           {
                airlock = new Airlock();
                airlocks.Add(line_vals[2], airlock);
           }
           airlock.doors.Add(door);
        }
    }
}

public void Save()
{
    // Called when the program needs to save its state. Use
    // this method to save your state to the Storage field
    // or some other means. 
    // 
    // This method is optional and can be removed if not
    // needed.
}

public void Reset()
{
    text_panels = new List<TextPanel>();
    DBG="\n";
    String[] args = Me.CustomData.Split('\n');
    foreach(string line in args)
    {
        String[] line_vals = line.Split(':');
        if (line_vals.Length>1)
        {
            if (line_vals[0].Equals("Name", StringComparison.InvariantCultureIgnoreCase))
            {
                group_name = line_vals[1];
            }
        }
    }
    FindTextPanels();
    FindGyros();
    FindRotors();
    FindPanels();
    FindVents();
    FindDoors();

    //rotor = GridTerminalSystem.GetBlockWithName("Rotor_Vxxx") as IMyMotorStator;
}

public string formatTimeSpan(TimeSpan timespan)
{
    if (timespan.Days > 0)
    {
        return Math.Round(timespan.TotalDays,1).ToString() + " days";
    }
    if (timespan.Hours > 0)
    {
        return Math.Round(timespan.TotalHours,1).ToString() + " hours";
    }
    if (timespan.Minutes > 0)
    {
        return Math.Round(timespan.TotalMinutes,1).ToString() + " minutes";
    }
    return Math.Round(timespan.TotalSeconds).ToString() + " seconds";
}

public Double getSolarPower(IMySolarPanel panel)
{
    if (panel == null)
        return 0;
    string text_out = panel.DetailedInfo.Split('\n')[1].Split(' ')[2];
    Double pwr_out = Convert.ToDouble(text_out);

    dbg = "'"+panel.DetailedInfo.Split('\n')[1].Split(' ')[3]+"'";
    if (panel.DetailedInfo.Split('\n')[1].Split(' ')[3].Equals("W"))
    {
        dbg = "WWWW";
        pwr_out = 0;
    }
    return pwr_out;
}

public string CheckDoors(Airlock airlock)
{
    List<IMyDoor> doors = airlock.doors;
    IMyAirVent air_vent_inner = airlock.vent_a;
    IMyAirVent air_vent_outer = airlock.vent_b;

    String return_string;

    if (air_vent_inner != null && air_vent_inner.IsWorking) 
    {
        if (air_vent_inner.GetOxygenLevel() > 0.1)
            airlock.vent_status_a = air_vent_inner.Status;
        else
            airlock.vent_status_a = VentStatus.Depressurized;
    }
    if (air_vent_outer != null && air_vent_outer.IsWorking) 
    {
        if (air_vent_outer.GetOxygenLevel() > 0.1)
            airlock.vent_status_b = air_vent_outer.Status; 
        else
            airlock.vent_status_b = VentStatus.Depressurized;
    }

    return_string = airlock.vent_status_a.ToString() + ' ' + airlock.vent_status_b.ToString(); 

    bool status = CheckStatus(airlock.vent_status_a,airlock.vent_status_b);

    foreach(TextPanel panel in airlock.panels)
    {
        var frame = panel.panel.DrawFrame();

        // All sprites must be added to the frame here
        if (panel.b)
        {
            DrawSprites(ref frame,panel.viewport,airlock.vent_status_b,status);
        }
        else
        {
            DrawSprites(ref frame,panel.viewport,airlock.vent_status_a,status);
        }
        // We are done with the frame, send all the sprites to the text panel
        frame.Dispose();
    }

    if (status)
    {
        foreach(IMyDoor door in doors)
        {
            return_string = return_string + ' ' + door.Status;
            door.ApplyAction("OnOff_On");
        }
    }
    else
    {
        foreach(IMyDoor door in doors)
        {
            if (door.Status == DoorStatus.Open || door.Status == DoorStatus.Opening)
            {
                door.ApplyAction("Open_Off");
            }
            else if (door.Status == DoorStatus.Closed)
            {
                door.ApplyAction("OnOff_Off");
            }
        }
    }
    return return_string;
}

public bool CheckStatus(VentStatus a, VentStatus b)
{
    if ((a == VentStatus.Depressurizing || a == VentStatus.Depressurized) && (b == VentStatus.Depressurizing || b == VentStatus.Depressurized))
    {    
        return true;
    }
    else if ((a == VentStatus.Pressurized) && (b == VentStatus.Pressurized))
    {
        return true;
    }
    //else if ((a == VentStatus.Pressurizing || a == VentStatus.Pressurized) && (b == VentStatus.Pressurizing || b == VentStatus.Pressurized))
    //{
    //    return true;
    //}
    return false;
}

void UpdateLCDText(IMyTextSurface surface, string text)
{
//    surface.ContentType = ContentType.TEXT_AND_IMAGE;
		//   	surface.FontSize = 1.0F;
		//	    surface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
		    	surface.WriteText(text);
}

// Drawing Sprites
public void DrawText(string text,ref MySpriteDrawFrame frame,Vector2 base_position,RectangleF viewport,Color color, Color background)
{
    var scale = new Vector2(viewport.Width/500,viewport.Height/500);
    var position =  ((new Vector2(0, 22) + base_position) * scale) + viewport.Position;
    MySprite sprite;

    if (background != null)
    {
    sprite = new MySprite()
    {
        Type = SpriteType.TEXTURE,
        Data = "SquareSimple",
        Position = position,
        Size = new Vector2(viewport.Width,45 * scale.Y),
        Color = background,
        Alignment = TextAlignment.LEFT /* Center the text on the position */,
        FontId = "White"
    };
    frame.Add(sprite);
    }
    position =  ((new Vector2(20, 2) + base_position) * scale) + viewport.Position;

    // Create our first line
    sprite = new MySprite()
    {
        Type = SpriteType.TEXT,
        Data = text,
        Position = position,
        RotationOrScale = 1.5f * Math.Min(scale.X,scale.Y),
        Color = color,
        Alignment = TextAlignment.LEFT /* Center the text on the position */,
        FontId = "DEBUG"
    };
    // Add the sprite to the frame
    frame.Add(sprite);
}

public void DrawBar(string type,double ratio, ref MySpriteDrawFrame frame,Vector2 base_position,RectangleF viewport,Color color)
{
    var scale = new Vector2(viewport.Width/500,viewport.Height/500);
    var position =  ((new Vector2(460, 2) + base_position) * scale) + viewport.Position;

    // Create our first line
    var sprite = new MySprite()
    {
        Type = SpriteType.TEXT,
        Data = ((int)(ratio * 100)).ToString(),
        Position = position,
        RotationOrScale = 1f * scale.X,
        Color = color,
        Alignment = TextAlignment.LEFT /* Center the text on the position */,
        FontId = "White"
    };
    // Add the sprite to the frame
    frame.Add(sprite);
    
    // Set up the initial position - and remember to add our viewport offset
    position = ((new Vector2(20, 20)  + base_position) * scale) + viewport.Position;
    
    // Create our first line
    sprite = new MySprite()
    {
        Type = SpriteType.TEXTURE,
        Data = type,
        Position = position,
        Size = new Vector2(50,50) * scale,
        Color = color,
        Alignment = TextAlignment.LEFT /* Center the text on the position */,
        FontId = "White"
    };
    // Add the sprite to the frame
    frame.Add(sprite);

    position = ((new Vector2(70, 20)  + base_position) * scale) + viewport.Position;;
    sprite = new MySprite()
    {
        Type = SpriteType.TEXTURE,
        Data = "SquareSimple",
        Position = position,
        Size = new Vector2(380,30) * scale,
        Color = new Color(color.R/4,color.G/4,color.B/4),
        Alignment = TextAlignment.LEFT /* Center the text on the position */,
        FontId = "White"
    };
    // Add the sprite to the frame
    frame.Add(sprite);

    if (ratio > 1.0) ratio = 1.0;

    sprite = new MySprite()
    {
        Type = SpriteType.TEXTURE,
        Data = "SquareSimple",
        Position = position,
        Size = new Vector2((float)(380*ratio),30) * scale,
        Color = color,
        Alignment = TextAlignment.LEFT /* Center the text on the position */,
        FontId = "White"
    };
    // Add the sprite to the frame
    frame.Add(sprite);
}

// Drawing Sprites
public void DrawSprites(ref MySpriteDrawFrame frame,RectangleF viewport,VentStatus vent_status,bool status)
{
    var position =  new Vector2(viewport.Size.X - 10, viewport.Size.Y/2 - 20) + viewport.Position;;

    if (viewport.Size.X < 500)
    {
        position.Y = position.Y + 30;
    }    
    // Create our first line
    var sprite = new MySprite()
    {
        Type = SpriteType.TEXT,
        Data = vent_status.ToString(),
        Position = position,
        RotationOrScale = 1.5f /* 80 % of the font's default size */,
        Color = Color.Yellow,
        Alignment = TextAlignment.RIGHT /* Center the text on the position */,
        FontId = "White"
    };
    if (vent_status == VentStatus.Depressurized)
    {
        sprite.Color = Color.Red;
    }
    else if (vent_status == VentStatus.Pressurized)
    {
        sprite.Color = Color.Green;
    }
    
    // Set up the initial position - and remember to add our viewport offset
    position = new Vector2(viewport.Size.X/4, viewport.Size.Y/2) + viewport.Position;
       
    if (viewport.Size.X < 500)
    {
        position.X = viewport.Size.X/2 + viewport.Position.X;
        position.Y = position.Y - 40;
        sprite.RotationOrScale = 1.4f;
    }

    // Add the sprite to the frame
    frame.Add(sprite);

    // Create our first line
    sprite = new MySprite()
    {
        Type = SpriteType.TEXTURE,
        Data = "Cross",
        Position = position,
        Size = viewport.Size/2,
        Color = Color.White,
        Alignment = TextAlignment.CENTER /* Center the text on the position */,
        FontId = "White"
    };

    if (status)
    {
        sprite.Data = "Arrow";
    }
    // Add the sprite to the frame
    frame.Add(sprite);
}

public void Main(string argument, UpdateType updateSource)
{
    if (argument.Equals("reset"))
    {
        Reset();
        if (rotor != null) rotor.SetValueFloat("Velocity", -0.5f);
        //gyro.GyroPower = 0.5F;
        if (gyro != null) gyro.Pitch = -0.1F;
        stop_scanning = 0;
        scanning = true;
    }

    float pwr_out = panel.CurrentOutput;// Convert.ToDouble(text_out);

    if (stop_scanning > 0)
    {
            //gyro.GyroPower = 0;
            stop_scanning--;
    }
    else if (scanning)
    {
        if (prev_power > pwr_out)
        {
            if (rotor != null) rotor.SetValueFloat("Velocity", 0.0f);
            if (gyro != null) gyro.Pitch = 0;
            scanning = false;
            stop_scanning = 10; // If we have reached max power stop for a period of time.
                                             // This stop a issue where the script would start scanning again after finding the best angle.
            prev_power = 0;
        }
    }
    else
    {
        if (prev_power > pwr_out)
        {
            if (rotor != null)  rotor.SetValueFloat("Velocity", 0.1f);
            //gyro.GyroPower = 0.5F;
            if (gyro != null) gyro.Pitch = 0.005F;
            scanning = true;
        }

    }
    prev_power = pwr_out;

    string battery_text = "";
    List<IMyBatteryBlock> blocks = new List<IMyBatteryBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(blocks);
    
    float current_stored_power = 0, max_stored_power = 0;
    float current_stored_oxygen = 0, max_stored_oxygen = 0;
    float current_stored_hydrogen = 0, max_stored_hydrogen = 0;
    float current_uranium = 0;

    foreach(IMyBatteryBlock battery in blocks)
    {
        if (battery.CubeGrid == Me.CubeGrid) {
            battery_text = battery_text + battery.CustomName + " - " +(battery.DetailedInfo.Split('\n')[6]) + '\n';
        current_stored_power += battery.CurrentStoredPower;
        max_stored_power += battery.MaxStoredPower; }
    }

    String tanks_text = "\nTanks\n-----\n";

    List<IMyGasTank> blocks_tanks = new List<IMyGasTank>();
    GridTerminalSystem.GetBlocksOfType<IMyGasTank>(blocks_tanks);
    
    foreach(IMyGasTank tank in blocks_tanks)
    {
        if (tank.CubeGrid == Me.CubeGrid)
        {
            if (tank.BlockDefinition.SubtypeId.Contains("Hydro"))
            {
                max_stored_hydrogen += tank.Capacity;
                current_stored_hydrogen += (float)(tank.Capacity * tank.FilledRatio);
            }
            else
            {
                max_stored_oxygen += tank.Capacity;
                current_stored_oxygen += (float)(tank.Capacity * tank.FilledRatio);
            }
            tanks_text = tanks_text + tank.CustomName + " - " +Math.Round(tank.FilledRatio*100,1) + "% - " + Math.Round(tank.Capacity) + '\n';
        }
    }

    // Hydrogen usage.
    {
        DateTime now = DateTime.UtcNow;
        TimeSpan timespan = now - prev_time;
        
        if (timespan > UPDATE_INTERVAL)
        {
            double hydrogen_used_per_second = (prev_hydrogen - current_stored_hydrogen) / timespan.TotalSeconds; 

            if (hydrogen_used_per_second > 0)
            {
                TimeSpan time_remaining = TimeSpan.FromSeconds(current_stored_hydrogen / hydrogen_used_per_second);

                current_hydrogen_time = formatTimeSpan(time_remaining);
            }
            else if (hydrogen_used_per_second < 0)
            {
                TimeSpan time_remaining = TimeSpan.FromSeconds((max_stored_hydrogen - current_stored_hydrogen) / hydrogen_used_per_second);
                current_hydrogen_time = formatTimeSpan(-time_remaining) + " till full";
            }

            else
            {
                current_hydrogen_time = "---";
            }
            prev_hydrogen = current_stored_hydrogen;
            prev_time = now;
        }
        tanks_text = tanks_text + current_hydrogen_time;
    }
    String reactors_text = "\nReactors\n-----\n";

    List<IMyReactor> blocks_reactors = new List<IMyReactor>();
    GridTerminalSystem.GetBlocksOfType<IMyReactor>(blocks_reactors);
    
    foreach(IMyReactor reactor in blocks_reactors)
    {
        if (reactor.CubeGrid == Me.CubeGrid)
        {
            reactors_text = reactors_text + reactor.CustomName + " - " + reactor.DetailedInfo.Split('\n')[2]  + '\n';
            if (reactor.GetInventory(0).GetItemAt(0).HasValue)
            {
                current_uranium += (float)reactor.GetInventory(0).GetItemAt(0).Value.Amount;
                reactors_text = reactors_text + " - " + reactor.GetInventory(0).GetItemAt(0).Value.Amount +  '\n';
            }
            else reactors_text = reactors_text + " * Warning no fuel!";
        }
    }

    string solar_text = "";

    if (rotor != null) solar_text = "Rotor- Pitch:" +  (rotor.Angle / (float)Math.PI * 180f) + " Power:" + rotor.GetValueFloat("Velocity") + "\n";
    if (gyro != null) solar_text = "Rotor- Gyro:" +  gyro.Pitch + " ";
    solar_text = solar_text +  "Solar: "+pwr_out*panels*1000 +" kW\n";

    IMyTextSurface mesurface1 = Me.GetSurface(0);
    UpdateLCDText(mesurface1,solar_text +stop_scanning + DBG );

    foreach(TextPanel text_panel in text_panels)
    {
        if ((text_panel.include_battery & 32) != 0)
        {
            var frame = text_panel.panel.DrawFrame();

            // All sprites must be added to the frame here
            DrawText("Power/Ox Time:"+DateTime.Now.ToString("HH:mm"),ref frame,new Vector2(0,0),text_panel.viewport,text_panel.panel.ScriptBackgroundColor,text_panel.panel.ScriptForegroundColor);

            DrawBar("IconHydrogen",(current_stored_hydrogen/max_stored_hydrogen),ref frame,new Vector2(0,50),text_panel.viewport,text_panel.panel.ScriptForegroundColor);
            DrawText(current_hydrogen_time,ref frame,new Vector2(0,100),text_panel.viewport,text_panel.panel.ScriptForegroundColor,Color.Black);

            DrawBar("IconOxygen",(current_stored_oxygen/max_stored_oxygen),ref frame,new Vector2(0,150),text_panel.viewport,text_panel.panel.ScriptForegroundColor);
            DrawBar("IconEnergy",(current_stored_power/max_stored_power),ref frame,new Vector2(0,200),text_panel.viewport,text_panel.panel.ScriptForegroundColor);
            DrawBar("MyObjectBuilder_Ingot/Uranium",current_uranium/100,ref frame,new Vector2(0,250),text_panel.viewport,text_panel.panel.ScriptForegroundColor);
            // We are done with the frame, send all the sprites to the text panel
            frame.Dispose();
        }
        string text = "";
        if ((text_panel.include_battery & 2) != 0)
        {
            text = "Solar\n--------\n"+solar_text;
        }
        if ((text_panel.include_battery & 1) != 0)
        {
            text = text + "\nBattery\n--------- "+"\n"+ battery_text;
        }
        else
        {
            text = text + stop_scanning;
        }        
        if ((text_panel.include_battery & 4) != 0)
        {
            text = text +tanks_text;
        }
        if ((text_panel.include_battery & 8) != 0)
        {
            text = text + reactors_text;
        }
        foreach(KeyValuePair<string, Airlock> kvp in airlocks)
        {
            //text = text + kvp.Key + "\n -- " + 
CheckDoors(kvp.Value);// + "\n";
        }
        UpdateLCDText(text_panel.panel,text);
    }    
}