String group_name = "";
String SCRIPT_PREFIX = "PowerCTL";
List <TextPanel> text_panels;
string DBG = "";

public class TextPanel
{
    public string name = null;
    public IMyTextSurface panel;
    public int include_battery;
}


bool scanning = true;
int stop_scanning = 0;
Double prev_power = 0;
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

           TextPanel panel = new TextPanel();
           panel.panel = text_panel;
           panel.include_battery = Convert.ToInt32(line_vals[3]);

            text_panels.Add(panel);
        }
    }

    List<IMyCockpit> blocks_cp = new List<IMyCockpit>();
    GridTerminalSystem.GetBlocksOfType<IMyCockpit>(blocks_cp);

    foreach(IMyCockpit text_panel in blocks_cp)
    {
        String[] line_vals = text_panel.CustomData.Split('\n')[0].Split(':');
        if (line_vals.Length>1 && line_vals[1].Equals(group_name) && line_vals[0].Equals(SCRIPT_PREFIX))
        {
           
           DBG = DBG + '\n' + text_panel.CustomName + " - " +  line_vals[1] + " - " + line_vals[2];
           DBG = DBG + " - " +line_vals[3];

           TextPanel panel = new TextPanel();
           panel.panel = text_panel.GetSurface(Convert.ToInt32(line_vals[4]));
           panel.include_battery = Convert.ToInt32(line_vals[3]);

            text_panels.Add(panel);
        }
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

    //rotor = GridTerminalSystem.GetBlockWithName("Rotor_Vxxx") as IMyMotorStator;
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

void UpdateLCDText(IMyTextSurface surface, string text)
{
//    surface.ContentType = ContentType.TEXT_AND_IMAGE;
		//   	surface.FontSize = 1.0F;
		//	    surface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
		    	surface.WriteText(text);
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

    Double pwr_out = getSolarPower(panel);// Convert.ToDouble(text_out);

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
    
    foreach(IMyBatteryBlock battery in blocks)
    {
        if (battery.CubeGrid == Me.CubeGrid) 
            battery_text = battery_text + battery.CustomName + " - " +(battery.DetailedInfo.Split('\n')[6]) + '\n';
    }

    String tanks_text = "\nTanks\n-----\n";

    List<IMyGasTank> blocks_tanks = new List<IMyGasTank>();
    GridTerminalSystem.GetBlocksOfType<IMyGasTank>(blocks_tanks);
    
    foreach(IMyGasTank tank in blocks_tanks)
    {
        if (tank.CubeGrid == Me.CubeGrid)
        tanks_text = tanks_text + tank.CustomName + " - " +Math.Round(tank.FilledRatio*100,1) + "% - " + Math.Round(tank.Capacity) + '\n';
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
                reactors_text = reactors_text + " - " + reactor.GetInventory(0).GetItemAt(0).Value.Amount +  '\n';
            else reactors_text = reactors_text + " * Warning no fuel!";
        }
    }

    string solar_text = "";

    if (rotor != null) solar_text = "Rotor- Pitch:" +  (rotor.Angle / (float)Math.PI * 180f) + " Power:" + rotor.GetValueFloat("Velocity") + "\n";
    if (gyro != null) solar_text = "Rotor- Gyro:" +  gyro.Pitch + " ";
    solar_text = solar_text +  "Solar: "+pwr_out*panels +" kW\n";

    IMyTextSurface mesurface1 = Me.GetSurface(0);
    UpdateLCDText(mesurface1,solar_text +stop_scanning + DBG );

    foreach(TextPanel text_panel in text_panels)
    {
        string text = "";
        if ((text_panel.include_battery & 2) != 0)
        {
            text = "Solar\n--------\n"+solar_text;
        }
        if ((text_panel.include_battery & 1) != 0)
        {
            text = text + "\nBattery\n---------\n"+ battery_text;
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
        UpdateLCDText(text_panel.panel,text);
    }
}