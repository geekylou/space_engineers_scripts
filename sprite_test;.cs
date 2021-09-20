IMyTextSurface _drawingSurface;
RectangleF _viewport;

String group_name = "test";
String SCRIPT_PREFIX = "PowerCTL";
List <TextPanel> text_panels;

String DBG = "";

public class TextPanel
{
    public string name = null;
    public IMyTextSurface panel;
    public int include_battery;
    public RectangleF viewport;
}

public Program()
{
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

    _drawingSurface = Me.GetSurface(0);

    // Calculate the viewport offset by centering the surface size onto the texture size
    _viewport = new RectangleF(
        (_drawingSurface.TextureSize - _drawingSurface.SurfaceSize) / 3f,
        _drawingSurface.SurfaceSize
    );
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

public void Main(string argument, UpdateType updateSource)
{
    // The main entry point of the script, invoked every time
    // one of the programmable block's Run actions are invoked,
    // or the script updates itself. The updateSource argument
    // describes where the update came from.
    // 
    // The method itself is required, but the arguments above
    // can be removed if not needed.
    IMyTextSurface mesurface1 = Me.GetSurface(0);
    //List<System.String> scripts = new List<System.String>();
    //mesurface1.GetSprites(scripts);

    text_panels = new List<TextPanel>();
    DBG="\n";

    FindTextPanels();

    //foreach(String str in scripts)
    //{
    //   DBG = DBG + str + "\n";
    //}

    mesurface1.WriteText(DBG);

    // Begin a new frame

    foreach(TextPanel panel in text_panels)
    {
        var frame = panel.panel.DrawFrame();

        // All sprites must be added to the frame here
        DrawSprites(ref frame,panel.viewport);

        // We are done with the frame, send all the sprites to the text panel
        frame.Dispose();
    }
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
           panel.viewport = new RectangleF(
                (text_panel.TextureSize - text_panel.SurfaceSize) / 3f,
                text_panel.SurfaceSize
            );
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

// Drawing Sprites
public void DrawSprites(ref MySpriteDrawFrame frame,RectangleF viewport)
{
    var position =  new Vector2(viewport.Size.X - 10, viewport.Size.Y/2 - 20) + viewport.Position;;

    // Create our first line
    var sprite = new MySprite()
    {
        Type = SpriteType.TEXT,
        Data = "Pressurized",
        Position = position,
        RotationOrScale = 1.5f /* 80 % of the font's default size */,
        Color = Color.Green,
        Alignment = TextAlignment.RIGHT /* Center the text on the position */,
        FontId = "White"
    };
    // Add the sprite to the frame
    frame.Add(sprite);
    
    // Set up the initial position - and remember to add our viewport offset
    position = new Vector2(viewport.Size.X/4, viewport.Size.Y/2 + viewport.Position.Y) ;
    
    // Create our first line
    sprite = new MySprite()
    {
        Type = SpriteType.TEXTURE,
        Data = "Arrow",
        Position = position,
        Size = viewport.Size/2,
        Color = Color.White,
        Alignment = TextAlignment.CENTER /* Center the text on the position */,
        FontId = "White"
    };
    // Add the sprite to the frame
    frame.Add(sprite);
}