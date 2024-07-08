using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Game;
public partial class Main : Node2D
{
    public bool lose = false;
    public override void _Ready()
    {
        PutTiles putTiles = new();
        AddChild(putTiles);
    }

    public override void _Process(double delta)
    {
    }
}

public partial class Player : CharacterBody2D
{
    //Resources
    public Texture2D character_texture = ResourceLoader.Load<Texture2D>("res://icon.svg");

    //Objects
    public Sprite2D character_sprite = new();

    public CollisionShape2D collisonCharacter = new();
    public RectangleShape2D collisionShape = new();

    public Camera2D camera = new();

    public RayCast2D RaycastInRight = new();
    public RayCast2D RaycastInLeft = new();
    public RayCast2D RaycastOutRight = new();
    public RayCast2D RaycastOutLeft = new();

    public Area2D AreaFoots = new();
    public CollisionShape2D CollisionArea = new();
    public RectangleShape2D AreaShape = new();

    //Constants
    public const float TILE_SIZE = 16 * 60;
    public const float PIXEL = 1 * 60;
    public const float SPIXEL = (float)(0.0625 * 60);
    public const float SSPIXEL = (float)(0.00390625 * 60);
    public const float SSSPIXEL = (float)(0.000244141 * 60);

    //Variables
    public bool stomped = false;
    public float grav;
    public float velx;
    public float vely;
    public float initvelx;
    public float max_ver_air;

    public Tween tween;

    enum Powerup { BASE, MUSHROM, FIRE_FLOWER, STAR, ONEUP }
    Powerup ActualPowerup = Powerup.BASE;

    //Functions
    public override void _Ready()
    {
        //Array array = GetParent().GetChildren();
        InitObjects();
        InitPlayer();
        AreaFoots.AreaEntered += Stomp;
    }

    public void InitObjects()
    {
        InitSprite();
        InitCollision();
        InitCamera();
        InitRaycast();
        InitArea();
    }

    public void InitArea()
    {
        AreaFoots.Position = new Vector2(0, 7);
        AreaFoots.AddChild(CollisionArea);
        CollisionArea.Shape = AreaShape;
        AreaShape.Size = new Vector2I(14, 1);
    }

    public void InitSprite()
    {
        character_sprite.Texture = character_texture;
        character_sprite.Scale = new Vector2((float)0.125, (float)0.125);
    }

    public void InitCollision()
    {
        collisionShape.Size = new Vector2(12, 14);
        collisonCharacter.Shape = collisionShape;
    }

    public void InitCamera()
    {
        camera.Offset = new Vector2(0, (float)(16 * -4.5));
        camera.LimitBottom = 313;
        camera.LimitLeft = 16;
        camera.LimitTop = 73;
        camera.DragHorizontalEnabled = true;
        camera.DragRightMargin = 0;
        camera.DragLeftMargin = (float)0.1;
    }

    public void InitRaycast()
    {
        RaycastOutLeft.TargetPosition = new Vector2(0, -8);
        RaycastOutLeft.Position = new Vector2(-8, 0);


        RaycastInLeft.TargetPosition = new Vector2(0, -8);
        RaycastInLeft.Position = new Vector2(-3, 0);


        RaycastInRight.TargetPosition = new Vector2(0, -8);
        RaycastInRight.Position = new Vector2(3, 0);


        RaycastOutRight.TargetPosition = new Vector2(0, -8);
        RaycastOutRight.Position = new Vector2(8, 0);
    }

    public void InitPlayer()
    {
        AddChild(character_sprite);
        AddChild(collisonCharacter);
        AddChild(camera);
        AddChild(AreaFoots);
        AddRaycast();
    }

    public void AddRaycast()
    {
        AddChild(RaycastOutLeft);
        AddChild(RaycastInLeft);
        AddChild(RaycastOutRight);
        AddChild(RaycastInRight);
    }
    public override void _PhysicsProcess(double delta)
    {
        if (IsOnFloor())
        {
            for (int i = 0; i < GetSlideCollisionCount(); i++)
            {
                GodotObject collision = GetSlideCollision(i).GetCollider();
                if (collision is not GoombaEnemy)
                {
                    vely = 0;
                    grav = 0;
                    stomped = false;
                }
                else
                {
                    GD.Print(collision);
                }
            }
        }
        if (IsOnWall())
        {
            velx = 0;
        }
        Jump();
        vely += Gravity();
        Velocity = new Vector2(Run(), vely);
        GD.Print(Velocity);
        CornerEffect();
        MoveAndSlide();
    }

    public void CornerEffect()
    {
        if (RaycastInRight.IsColliding() || RaycastInLeft.IsColliding())
        {
            grav = 0;
            vely = 0;
        }
        if (RaycastOutLeft.IsColliding() && !RaycastInLeft.IsColliding())
        {
            if (Velocity.Y < 0)
            {
                while (TestMove(new Transform2D(0, Position), Vector2.Up))
                {
                    Position += new Vector2(1, 0);
                }
            }
        }
        else if (RaycastOutRight.IsColliding() && !RaycastInRight.IsColliding())
        {
            if (Velocity.Y < 0)
            {
                while (TestMove(new Transform2D(0, Position), Vector2.Up))
                {
                    Position -= new Vector2(1, 0);
                }
            }
        }
    }

    public void Jump()
    {
        if (Godot.Input.IsActionJustPressed("Space") && Mathf.Abs(velx) < 1 * PIXEL && IsOnFloor())
        {
            vely += -4 * PIXEL + -2 * SPIXEL;
            initvelx = Velocity.X;
        }
        else if (Godot.Input.IsActionJustPressed("Space") && 1 * PIXEL < Mathf.Abs(velx) && Mathf.Abs(velx) < (2 * PIXEL + 4 * SPIXEL + 15 * SSPIXEL + 15 * SSSPIXEL) && IsOnFloor())
        {
            vely += -4 * PIXEL + -2 * SPIXEL;
            initvelx = Velocity.X;
        }
        else if (Godot.Input.IsActionJustPressed("Space") && Mathf.Abs(velx) > (2 * PIXEL + 5 * SPIXEL) && IsOnFloor())
        {
            vely += -5 * PIXEL;
            initvelx = Velocity.X;
        }
    }

    public float Gravity()
    {
        if (!stomped)
        {
            if (Mathf.Abs(initvelx) < 1 * PIXEL)
            {
                if (Godot.Input.IsActionPressed("Space") && vely < 0)
                {
                    grav = 2 * SPIXEL;
                }
                else
                {
                    grav = 7 * SPIXEL;
                }
                if (grav > (4 * PIXEL + 8 * SPIXEL))
                {
                    grav = 4 * PIXEL;
                }
            }
            else if (1 * PIXEL < Mathf.Abs(initvelx) && Mathf.Abs(initvelx) < (2 * PIXEL + 4 * SPIXEL + 15 * SSPIXEL + 15 * SSSPIXEL))
            {
                if (Godot.Input.IsActionPressed("Space") && vely < 0)
                {
                    grav = 1 * SPIXEL + 14 * SSPIXEL;
                }
                else
                {
                    grav = 6 * SPIXEL;
                }
                if (grav > (4 * PIXEL + 8 * SPIXEL))
                {
                    grav = 4 * PIXEL;
                }
            }
            else if (Mathf.Abs(initvelx) > (2 * PIXEL + 5 * SPIXEL))
            {
                if (Godot.Input.IsActionPressed("Space") && vely < 0)
                {
                    grav = 2 * SPIXEL + 8 * SSPIXEL;
                }
                else
                {
                    grav = 9 * SPIXEL;
                }
                if (grav > (4 * PIXEL + 8 * SPIXEL))
                {
                    grav = 4 * PIXEL;
                }
            }
        }
        else if (stomped)
        {
            grav = 7 * SPIXEL;
        }
        return grav;
    }

    public void Bounce()
    {
        grav = 0;
        vely = -4 * PIXEL;
    }
    public void Damage()
    {
        switch (ActualPowerup)
        {
            case Powerup.BASE:
                Death();
                break;
        }
    }

    public void Stomp(Area2D area)
    {
        if (area.GetParent() is GoombaEnemy)
        {
            GoombaEnemy goomba = area.GetParent() as GoombaEnemy;
            stomped = true;
            goomba.Stomped();
            GD.Print("Stomped!");
        }
    }

    public void Death()
    {
        tween = GetTree().CreateTween();
        tween.TweenProperty(this, "position", Position + new Vector2(0, -7 * TILE_SIZE / 60), 0.5);
        tween.Chain().TweenProperty(this, "position", Position + new Vector2(0, 15 * TILE_SIZE / 60), 1);
        collisionShape.Size = new Vector2(1, 1);
        AreaFoots.QueueFree();
        SetPhysicsProcess(false);
    }

    public float Run()
    {
        if (IsOnFloor())
        {
            velx = RunFloor();
        }
        else if (!IsOnFloor())
        {
            velx = RunMidAir();
        }
        return velx;
    }
    public float RunFloor()
    {
        if (Mathf.Abs(velx) > 1 * PIXEL + 9 * SPIXEL && !Godot.Input.IsActionPressed("Shift"))
        {
            velx = (1 * PIXEL + 9 * SPIXEL) * Mathf.Sign(velx);
        }
        else if (Mathf.Abs(velx) > 2 * PIXEL + 9 * SPIXEL && Godot.Input.IsActionPressed("Shift"))
        {
            velx = (2 * PIXEL + 9 * SPIXEL) * Mathf.Sign(velx);
        }

        if (Godot.Input.IsActionPressed("A") && Velocity.X > 0)
        {
            velx -= 9 * SPIXEL;
        }
        else if (Godot.Input.IsActionPressed("D") && Velocity.X < 0)
        {
            velx += 9 * SPIXEL;
        }

        if (Godot.Input.IsActionPressed("D") && !Godot.Input.IsActionPressed("Shift"))
        {
            velx += 9 * SSPIXEL + 8 * SSSPIXEL;
        }
        else if (Godot.Input.IsActionPressed("A") && !Godot.Input.IsActionPressed("Shift"))
        {
            velx -= 9 * SSPIXEL + 8 * SSSPIXEL;
        }

        if (Godot.Input.IsActionPressed("D") && Godot.Input.IsActionPressed("Shift"))
        {
            velx += 14 * SSPIXEL + 4 * SSSPIXEL;
        }
        else if (Godot.Input.IsActionPressed("A") && Godot.Input.IsActionPressed("Shift"))
        {
            velx -= 14 * SSPIXEL + 4 * SSSPIXEL;
        }

        if (!Godot.Input.IsActionPressed("A") && !Godot.Input.IsActionPressed("D"))
        {
            velx -= Mathf.Min(Mathf.Abs(velx), 13 * SSPIXEL) * Mathf.Sign(velx);
        }

        return velx;
    }

    public float RunMidAir()
    {
        if (Velocity.X >= 0)
        {
            if (velx < (1 * PIXEL + 10 * SPIXEL))
            {
                max_ver_air = 1 * PIXEL + 9 * SPIXEL;

                if (Godot.Input.IsActionPressed("A"))
                {
                    velx -= 9 * SSPIXEL + 8 * SSSPIXEL;
                }
                else if (Godot.Input.IsActionPressed("D"))
                {
                    velx += 13 * SSPIXEL;
                }

                if (velx > max_ver_air)
                {
                    velx = max_ver_air;
                }

            }
            else if (velx >= (1 * PIXEL + 10 * SPIXEL))
            {
                max_ver_air = 2 * PIXEL + 9 * SPIXEL;

                if (Godot.Input.IsActionPressed("A"))
                {
                    velx -= 14 * SSPIXEL + 4 * SSSPIXEL;
                }

                else if (Godot.Input.IsActionPressed("D"))
                {
                    velx += 14 * SSPIXEL + 4 * SSSPIXEL;
                }

                if (velx > max_ver_air)
                {
                    velx = max_ver_air;
                }
            }
        }

        else if (Velocity.X < 0)
        {
            if (Mathf.Abs(velx) < (1 * PIXEL + 10 * SPIXEL))
            {
                max_ver_air = -1 * PIXEL + -9 * SPIXEL;

                if (Godot.Input.IsActionPressed("A"))
                {
                    velx -= 9 * SSPIXEL + 8 * SSSPIXEL;
                }

                else if (Godot.Input.IsActionPressed("D"))
                {
                    velx += 13 * SSPIXEL;
                }
            }

            else if (Mathf.Abs(velx) >= (1 * PIXEL + 10 * SPIXEL))
            {
                max_ver_air = -2 * PIXEL + -9 * SPIXEL;


                if (Godot.Input.IsActionPressed("A"))
                {
                    velx -= 14 * SSPIXEL + 4 * SSSPIXEL;
                }

                else if (Godot.Input.IsActionPressed("D"))
                {
                    velx += 14 * SSPIXEL + 4 * SSSPIXEL;
                }
            }

            if (Mathf.Abs(velx) > Mathf.Abs(max_ver_air))
            {
                velx = max_ver_air;
            }

        }
        return velx;
    }
}

public partial class GoombaEnemy : CharacterBody2D
{
    //Resources
    public Texture2D goombaTexture = ResourceLoader.Load<Texture2D>("res://goomba.png");
    public Texture2D stompedTexture = ResourceLoader.Load<Texture2D>("res://goomba_stomped.png");

    //Objects
    public Sprite2D goombaSprite = new();

    public CollisionShape2D collisonSprite = new();
    public RectangleShape2D collisionShape = new();

    public RayCast2D RaycastRight = new();
    public RayCast2D RaycastLeft = new();

    public Area2D areaHurt = new();
    public CollisionShape2D collisionHurt = new();
    public RectangleShape2D shapeHurt = new();

    public VisibleOnScreenEnabler2D OnScreen = new();
    public Rect2 OnScreenRect = new(-56, 0, 112, 1);

    //Constants
    public const float TILE_SIZE = 16 * 60;
    public const float PIXEL = 1 * 60;
    public const float SPIXEL = (float)(0.0625 * 60);
    public const float SSPIXEL = (float)(0.00390625 * 60);
    public const float SSSPIXEL = (float)(0.000244141 * 60);

    //Variables
    public float grav;
    public float velx;
    public float vely;
    public float max_ver_air;
    public int direction = 1;

    public Player player;

    //Functions

   public void StartGoomba()
    {
        StartSprite();
        StartCollision();
        StartRaycast();
        StartHurtBox();
        StartEnabler();
    }

    public void StartSprite()
    {
        goombaSprite.Texture = goombaTexture;
    }

    public void StartCollision()
    {
        collisionShape.Size = new Vector2(10, 15);
        collisonSprite.Shape = collisionShape;
    }

    public void StartRaycast()
    {
        RaycastLeft.TargetPosition = new Vector2(-8, 0);
        RaycastRight.TargetPosition = new Vector2(8, 0);
    }

    public void StartHurtBox()
    {
        areaHurt.Position = new Vector2(0, -7);
        shapeHurt.Size = new Vector2(12, 1);
        collisionHurt.Shape = shapeHurt;
        areaHurt.AddChild(collisionHurt);
    }

    public void StartEnabler()
    {
        OnScreen.Rect = OnScreenRect;
    }

    public void AddChilds()
    {
        AddChild(goombaSprite);
        AddChild(OnScreen);
        AddChild(areaHurt);
        AddChild(RaycastLeft);
        AddChild(RaycastRight);
        AddChild(collisonSprite);
    }

    public override void _Ready()
    {
        StartGoomba();
        AddChilds();
    }

    public void MapReady()
    {
        foreach (var i in GetParent().GetChildren())
        {
            if (i is Player)
            {
                player = i as Player;
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        GoombaCollision();
        OnLose();
        Velocity = new Vector2(Movement(), Gravity());
        MoveAndSlide();
    }

    public void OnLose()
    {
        if (GetParent<Main>().lose)
        {
            SetPhysicsProcess(false);
        }
    }

    public float Movement()
    {
        velx = -13 * SPIXEL * direction;
        return velx;
    }

    public float Gravity()
    {
        if (!IsOnFloor())
        {
            vely += 7 * SPIXEL;
            return vely;
        }
        else
        {
            vely = 0;
            return vely;
        }
    }

    public void GoombaCollision()
    {
        if (RaycastRight.IsColliding())
        {
            if (RaycastRight.GetCollider() is TileMap || RaycastRight.GetCollider() is GoombaEnemy)
            {
                direction = 1;
            }
            else if (RaycastRight.GetCollider() is Player)
            {
                player.Damage();
                GetParent<Main>().lose = true;
            }
        }

        else if (RaycastLeft.IsColliding())
        {
            if (RaycastLeft.GetCollider() is TileMap || RaycastLeft.GetCollider() is GoombaEnemy)
            {
                direction = -1;
            }
            else if (RaycastLeft.GetCollider() is Player)
            {
                player.Damage();
                GetParent<Main>().lose = true;
            }
        }
    }

    public async void Stomped()
    {
        player.Bounce();
        goombaSprite.Texture = stompedTexture;
        collisonSprite.QueueFree();
        areaHurt.QueueFree();
        SetPhysicsProcess(false);
        RaycastLeft.QueueFree();
        RaycastRight.QueueFree();
        await ToSignal(GetTree().CreateTimer(0.75), "timeout");
        QueueFree();
    }
}

public partial class PutTiles : TileMap
{
    private TileSet tileset = new();
    private TileSetAtlasSource atlasSource = new();
    private Texture2D tiles = ResourceLoader.Load<Texture2D>("res://tilesheet.png");
    public Dictionary<string, Vector2I> converter = new(4);

    public override void _Ready()
    {
        converter.Add("?", Vector2I.Zero);
        converter.Add("#", new Vector2I(1, 0));
        converter.Add("D", new Vector2I(2, 0));
        converter.Add(" ", new Vector2I(-1, -1));
        InitTileset();
        CreateLevel();
    }

    public void CreateLevel()
    {
        int cont = 0;
        string level;
        String source = Godot.ProjectSettings.GlobalizePath("res://");
        string path = source + "/1-1.txt";
        Vector2I coordsI = new(0, 0);
        using StreamReader nameFile = new(path);
        {
            level = nameFile.ReadToEnd();
        }
        foreach (char i in level)
        {
            cont++;
            if (cont == level.Length)
            {
                foreach (Node j in GetParent().GetChildren())
                {
                    if(j is GoombaEnemy)
                    {
                        GoombaEnemy goombaEnemy = j as GoombaEnemy;
                        goombaEnemy.MapReady();
                    }
                }
            }
            string character = i.ToString();
            switch (character)
            {
                case "\n":
                    coordsI.X = 0;
                    coordsI.Y += 1;
                    break;
                case "G":
                    GoombaEnemy goombaEnemy = new();
                    CreateGoomba(goombaEnemy, coordsI);
                    coordsI.X += 1;
                    break;
                case "P":
                    Player player = new();
                    CreatePlayer(player, coordsI);
                    coordsI.X += 1;
                    break;
                case var _:
                    PutCell(coordsI, converter[character]);
                    coordsI.X += 1;
                    break;
            }
        }
    }

    public void CreateGoomba(GoombaEnemy goombaEnemy, Vector2I coordsI)
    {
        Vector2 coords = MapToLocal(coordsI);
        goombaEnemy.Position = coords;
        GetParent().AddChild(goombaEnemy);       
    }

    public void CreatePlayer(Player player, Vector2I coordsI)
    {
        Vector2 coords = MapToLocal(coordsI);
        player.Position = coords;
        GetParent().AddChild(player);
    }

    public void PutCell(Vector2I coords, Vector2I identifier)
    {
        SetCell(0, coords, 0, identifier);
    }

    public void InitTileset()
    {
        atlasSource.Texture = tiles;
        tileset.AddSource(atlasSource);
        tileset.AddPhysicsLayer();
        tileset.TileSize = new Vector2I(16, 16);
        CreatePhysics(new Vector2I(0, 0));
        CreatePhysics(new Vector2I(1, 0));
        CreatePhysics(new Vector2I(2, 0));
        TileSet = tileset;
    }

    public void CreatePhysics(Vector2I coords)
    {
        atlasSource.CreateTile(coords);
        Vector2[] tileCollision = new Vector2[4];
        tileCollision[0] = new Vector2(-8, -8);
        tileCollision[1] = new Vector2(-8, 8);
        tileCollision[2] = new Vector2(8, 8);
        tileCollision[3] = new Vector2(8, -8);
        TileData tiledat = atlasSource.GetTileData(coords, 0);
        tiledat.AddCollisionPolygon(0);
        tiledat.SetCollisionPolygonPoints(0, 0, tileCollision);
    }
}
