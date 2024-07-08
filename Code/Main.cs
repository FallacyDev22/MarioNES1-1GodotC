using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;

namespace Game;
public partial class Main : Node2D
{
	public bool lose = false;
	public PutTiles putTiles = new();
	public Player player;
	public override void _Ready()
	{
		AddChild(putTiles);
		foreach (Node i in GetChildren())
		{
			if (i is Player)
			{
				player = i as Player;
			}
		}
	}

	public override void _Process(double delta)
	{
	}
}

public partial class Player : CharacterBody2D
{
	//Resources
	private Texture2D BaseIdle = ResourceLoader.Load<Texture2D>("res://Sprites/Player/Base/BaseIdle.png");
	private Texture2D BaseRun1 = ResourceLoader.Load<Texture2D>("res://Sprites/Player/Base/BaseRun1.png");
	private Texture2D BaseRun2 = ResourceLoader.Load<Texture2D>("res://Sprites/Player/Base/BaseRun2.png");
	private Texture2D BaseRun3 = ResourceLoader.Load<Texture2D>("res://Sprites/Player/Base/BaseRun3.png");
	private Texture2D BaseJump = ResourceLoader.Load<Texture2D>("res://Sprites/Player/Base/BaseJump.png");

	private Texture2D BigJump = ResourceLoader.Load<Texture2D>("res://Sprites/Player/Mushroom/BigJump.png");
	private Texture2D BigRun1 = ResourceLoader.Load<Texture2D>("res://Sprites/Player/Mushroom/BigRun1.png");
	private Texture2D BigRun2 = ResourceLoader.Load<Texture2D>("res://Sprites/Player/Mushroom/BigRun2.png");
	private Texture2D BigRun3 = ResourceLoader.Load<Texture2D>("res://Sprites/Player/Mushroom/BigRun3.png");
	private Texture2D BigIdle = ResourceLoader.Load<Texture2D>("res://Sprites/Player/Mushroom/BigIdle.png");



	private Texture2D MarioDead = ResourceLoader.Load<Texture2D>("res://Sprites/Player/Base/MarioDead.png");

	//Objects
	private AnimatedSprite2D CharacterAnim = new();
	private SpriteFrames spriteFrames = new();

	private CollisionShape2D collisonCharacter = new();
	private RectangleShape2D collisionShape = new();

	private Camera2D camera = new();

	private RayCast2D RaycastInRight = new();
	private RayCast2D RaycastInLeft = new();
	private RayCast2D RaycastOutRight = new();
	private RayCast2D RaycastOutLeft = new();

	private Area2D AreaFoots = new();
	private CollisionShape2D CollisionArea = new();
	private RectangleShape2D AreaShape = new();

	//Constants
	private const float TILE_SIZE = 16 * 60;
	private const float PIXEL = 1 * 60;
	private const float SPIXEL = (float)(0.0625 * 60);
	private const float SSPIXEL = (float)(0.00390625 * 60);
	private const float SSSPIXEL = (float)(0.000244141 * 60);

	//Variables
	public bool stomped = false;
	public bool death = false;
	private bool JumpTime = true;
	private bool hurt = false;

	public float grav;
	public float velx;
	public float vely;
	public float initvelx;
	public float max_ver_air;

	private Tween tween;

	enum Powerup { BASE, MUSHROM, FIRE_FLOWER, STAR, ONEUP }
	Powerup ActualPowerup = Powerup.BASE;

	//Functions
	public override void _Ready()
	{
		InitObjects();
		InitPlayer();
		AreaFoots.AreaEntered += Stomp;
	}

	private void InitObjects()
	{
		InitAnim();
		InitCollision();
		InitCamera();
		InitRaycast();
		InitArea();
	}

	private void InitArea()
	{
		AreaFoots.Position = new Vector2(0, 7);
		AreaFoots.AddChild(CollisionArea);
		CollisionArea.Shape = AreaShape;
		AreaShape.Size = new Vector2I(14, 1);
	}

	private void InitAnim()
	{
		InitSpriteFrameBase();
		InitSpriteFrameBig();
		CharacterAnim.SpriteFrames = spriteFrames;
		CharacterAnim.Autoplay = "BaseIdle";
	}

	private void InitSpriteFrameBase()
	{
		spriteFrames.AddAnimation("BaseIdle");
		spriteFrames.AddAnimation("BaseRun");
		spriteFrames.AddAnimation("BaseJump");
		spriteFrames.AddAnimation("Dead");

		spriteFrames.AddFrame("BaseIdle", BaseIdle);

		spriteFrames.AddFrame("BaseRun", BaseRun1);
		spriteFrames.AddFrame("BaseRun", BaseRun2);
		spriteFrames.AddFrame("BaseRun", BaseRun3);

		spriteFrames.AddFrame("BaseJump", BaseJump);

		spriteFrames.AddFrame("Dead", MarioDead);
	}

	private void InitSpriteFrameBig()
	{
		spriteFrames.AddAnimation("BigIdle");
		spriteFrames.AddAnimation("BigRun");
		spriteFrames.AddAnimation("BigJump");

		spriteFrames.AddFrame("BigIdle", BigIdle);

		spriteFrames.AddFrame("BigRun", BigRun1);
		spriteFrames.AddFrame("BigRun", BigRun2);
		spriteFrames.AddFrame("BigRun", BigRun3);

		spriteFrames.AddFrame("BigJump", BigJump);
	}

	public void InitCollision()
	{
		collisionShape.Size = new Vector2(12, 14);
		collisonCharacter.Shape = collisionShape;
	}

	private void InitCamera()
	{
		camera.Offset = new Vector2(0, (float)(16 * -4.5));
		camera.LimitBottom = 313;
		camera.LimitLeft = 16;
		camera.LimitTop = 73;
		camera.DragHorizontalEnabled = true;
		camera.DragRightMargin = 0;
		camera.DragLeftMargin = (float)0.1015625;
	}

	private void InitRaycast()
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
	private void RaycastBig()
	{
		RaycastOutLeft.Position = new Vector2(-8, -8);

		RaycastInLeft.Position = new Vector2(-3, -8);

		RaycastInRight.Position = new Vector2(3, -8);

		RaycastOutRight.Position = new Vector2(8, -8);
	}

	private void InitPlayer()
	{
		AddChild(CharacterAnim);
		AddChild(collisonCharacter);
		AddChild(camera);
		AddChild(AreaFoots);
		AddRaycast();
	}

	private void AddRaycast()
	{
		AddChild(RaycastOutLeft);
		AddChild(RaycastInLeft);
		AddChild(RaycastOutRight);
		AddChild(RaycastInRight);
	}

	public override void _Process(double delta)
	{
		AnimController();
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
		CoyoteTime();
		Jump();
		vely += Gravity();
		Velocity = new Vector2(Run(), vely);
		CornerEffect();
		MoveAndSlide();
	}


	private void AnimController()
	{
		float velframes = ObtainVelFrame(Mathf.Abs(Velocity.X));

		if (ActualPowerup == Powerup.BASE)
		{
			if (!death)
			{
				if (Velocity.Y == 0)
				{
					if (Velocity.X != 0)
					{
						if (Velocity.X > 0)
						{
							spriteFrames.SetAnimationSpeed("BaseRun", velframes);
							CharacterAnim.Play("BaseRun");
							CharacterAnim.FlipH = false;
						}
						else
						{
							spriteFrames.SetAnimationSpeed("BaseRun", velframes);
							CharacterAnim.Play("BaseRun");
							CharacterAnim.FlipH = true;
						}
					}
					else
					{
						CharacterAnim.Play("BaseIdle");
					}
				}
				else
				{
					CharacterAnim.Play("BaseJump");
				}
			}
			else if (death)
			{
				CharacterAnim.Play("Dead");
			}
		}
		else if (ActualPowerup == Powerup.MUSHROM)
		{
			if (!death)
			{
				if (Velocity.Y == 0)
				{
					if (Velocity.X != 0)
					{
						if (Velocity.X > 0)
						{
							spriteFrames.SetAnimationSpeed("BigRun", velframes);
							CharacterAnim.Play("BigRun");
							CharacterAnim.FlipH = false;
						}
						else
						{
							spriteFrames.SetAnimationSpeed("BigRun", velframes);
							CharacterAnim.Play("BigRun");
							CharacterAnim.FlipH = true;
						}
					}
					else
					{
						CharacterAnim.Play("BigIdle");
					}
				}
				else
				{
					CharacterAnim.Play("BigJump");
				}
			}
		}
	}

	static private float ObtainVelFrame(float current_vel)
	{
		float maxframevel = 18f;
		float maxvel = 154f;
		if (current_vel < 40f)
		{
			return 4f;
		}
		return ((current_vel * maxframevel) / maxvel);
	}

	private void CornerEffect()
	{
		if (RaycastInRight.IsColliding() || RaycastInLeft.IsColliding())
		{
			grav = 0;
			vely = 0;
			if (RaycastInRight.IsColliding() && !RaycastInLeft.IsColliding())
			{
				GetParent<Main>().putTiles.MoveTile(Position - new Vector2(-4, 16));
			}
			else if (!RaycastInRight.IsColliding() && RaycastInLeft.IsColliding())
			{
				GetParent<Main>().putTiles.MoveTile(Position - new Vector2(4, 16));
			}
			else
			{
				GetParent<Main>().putTiles.MoveTile(Position - new Vector2(0, 16));
			}
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

	private async void CoyoteTime()
	{
		if (IsOnFloor())
		{
			JumpTime = true;
		}
		else
		{
			await ToSignal(GetTree().CreateTimer(0.15f), "timeout");
			JumpTime = false;
		}
	}

	private void Jump()
	{
		if (Godot.Input.IsActionJustPressed("Space") && Mathf.Abs(velx) < 1 * PIXEL && JumpTime)
		{
			JumpTime = false;
			grav = 0;
			vely = -4 * PIXEL + -2 * SPIXEL;
			initvelx = Velocity.X;
		}
		else if (Godot.Input.IsActionJustPressed("Space") && 1 * PIXEL < Mathf.Abs(velx) && Mathf.Abs(velx) < (2 * PIXEL + 4 * SPIXEL + 15 * SSPIXEL + 15 * SSSPIXEL) && JumpTime)
		{
			JumpTime = false;
			grav = 0;
			vely = -4 * PIXEL + -2 * SPIXEL;
			initvelx = Velocity.X;
		}
		else if (Godot.Input.IsActionJustPressed("Space") && Mathf.Abs(velx) > (2 * PIXEL + 5 * SPIXEL) && JumpTime)
		{
			JumpTime = false;
			grav = 0;
			vely = -5 * PIXEL;
			initvelx = Velocity.X;
		}
	}

	private float Gravity()
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
		HurtTimer();

		if (hurt == false)
		{
			AreaFoots.Monitorable = false;
			AreaFoots.Monitoring = false;
			switch (ActualPowerup)
			{
				case Powerup.BASE:
					Death();
					GetParent<Main>().lose = true;
					break;
				case Powerup.MUSHROM:
					CharacterAnim.Modulate = new Color(1f, 1f, 1f, 0.5f);

					InitRaycast();
					InitArea();
					InitCollision();
					ActualPowerup = Powerup.BASE;
					hurt = true;
					break;
			}
		}
	}

	private async void HurtTimer()
	{
		await ToSignal(GetTree().CreateTimer(0.75f), "timeout");
		hurt = false;
		AreaFoots.Monitorable = true;
		AreaFoots.Monitoring = true;
		CharacterAnim.Modulate = new Color(1f, 1f, 1f, 1f);
	}

	public void Stomp(Area2D area)
	{
		if (area.GetParent() is GoombaEnemy)
		{
			GoombaEnemy goomba = area.GetParent() as GoombaEnemy;
			stomped = true;
			goomba.Stomped();
		}
	}

	public void Death()
	{
		death = true;
		tween = GetTree().CreateTween();
		tween.TweenProperty(this, "position", Position + new Vector2(0, -7 * TILE_SIZE / 60), 0.5);
		tween.Chain().TweenProperty(this, "position", Position + new Vector2(0, 15 * TILE_SIZE / 60), 1);
		collisionShape.Size = new Vector2(1, 1);
		AreaFoots.QueueFree();
		SetPhysicsProcess(false);
	}

	private float Run()
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
	private float RunFloor()
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

	private float RunMidAir()
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

	public void Mushroom()
	{
		ActualPowerup = Powerup.MUSHROM;
		collisionShape.Size = new Vector2(12, 30);
		AreaFoots.Position += new Vector2(0, 8);
		RaycastBig();
	}
}

public partial class GoombaEnemy : CharacterBody2D
{
	//Resources
	private Texture2D GoombaWalk1 = ResourceLoader.Load<Texture2D>("res://Sprites/GoombaWalk/GoombaWalk1.png");
	private Texture2D GoombaWalk2 = ResourceLoader.Load<Texture2D>("res://Sprites/GoombaWalk/GoombaWalk2.png");
	private Texture2D stompedTexture = ResourceLoader.Load<Texture2D>("res://Sprites/goomba_stomped.png");

	//Objects
	private AnimatedSprite2D goombaAnim = new();
	private SpriteFrames GoombaFrames = new();

	private CollisionShape2D collisonSprite = new();
	private RectangleShape2D collisionShape = new();

	private RayCast2D RaycastRight = new();
	private RayCast2D RaycastLeft = new();

	private Area2D areaHurt = new();
	private CollisionShape2D collisionHurt = new();
	private RectangleShape2D shapeHurt = new();

	private VisibleOnScreenEnabler2D OnScreen = new();
	private Rect2 OnScreenRect = new(-56, 0, 112, 1);

	//Constants
	private const float SPIXEL = (float)(0.0625 * 60);

	//Variables
	private float grav;
	private float velx;
	private float vely;
	private float max_ver_air;
	private int direction = 1;

	private Player player;

	//Functions

	private void StartGoomba()
	{
		StartSprite();
		StartCollision();
		StartRaycast();
		StartHurtBox();
		StartEnabler();
	}

	private void StartSprite()
	{
		goombaAnim.Position = goombaAnim.Position with { Y = 1 };
		GoombaFrames.AddAnimation("Walk");
		GoombaFrames.AddAnimation("Die");
		GoombaFrames.AddFrame("Walk", GoombaWalk1);
		GoombaFrames.AddFrame("Walk", GoombaWalk2);
		GoombaFrames.AddFrame("Die", stompedTexture);
		goombaAnim.SpriteFrames = GoombaFrames;
		goombaAnim.Autoplay = "Walk";
	}

	private void StartCollision()
	{
		collisionShape.Size = new Vector2(10, 15);
		collisonSprite.Shape = collisionShape;
		CollisionLayer = 2;
		CollisionMask = 2;
	}

	private void StartRaycast()
	{
		RaycastLeft.TargetPosition = new Vector2(-8, 0);
		RaycastRight.TargetPosition = new Vector2(8, 0);
		RaycastRight.CollisionMask = 1;
		RaycastLeft.CollisionMask = 1;
	}

	private void StartHurtBox()
	{
		areaHurt.Position = new Vector2(0, -7);
		shapeHurt.Size = new Vector2(12, 1);
		collisionHurt.Shape = shapeHurt;
		areaHurt.CollisionMask = 1;
		areaHurt.AddChild(collisionHurt);
	}

	private void StartEnabler()
	{
		OnScreen.Rect = OnScreenRect;
	}

	private void AddChilds()
	{
		AddChild(goombaAnim);
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

	private void OnLose()
	{
		if (GetParent<Main>().lose)
		{
			SetPhysicsProcess(false);
		}
	}

	private float Movement()
	{
		velx = -13 * SPIXEL * direction;
		return velx;
	}

	private float Gravity()
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

	private void GoombaCollision()
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
			}
		}
	}

	public async void Stomped()
	{
		player.Bounce();
		goombaAnim.Play("Die");
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
	private Texture2D tiles = ResourceLoader.Load<Texture2D>("res://Sprites/tilesheet.png");
	private Texture2D box = ResourceLoader.Load<Texture2D>("res://Sprites/Box.png");
	private Texture2D brick = ResourceLoader.Load<Texture2D>("res://Sprites/Brick.png");
	private Texture2D block = ResourceLoader.Load<Texture2D>("res://Sprites/Block.png");
	private Texture2D boxempty = ResourceLoader.Load<Texture2D>("res://Sprites/BoxEmpty.png");

    public Dictionary<string, Vector2I> converter = new(4);

	public override void _Ready()
	{
		converter.Add("?", Vector2I.Zero);
        converter.Add("M", new Vector2I(4, 0));
        converter.Add("#", new Vector2I(1, 0));
		converter.Add("D", new Vector2I(2, 0));
		converter.Add(" ", new Vector2I(-1, -1));
		InitTileset();
		CreateLevel();
	}

	private void CreateLevel()
	{
		int cont = 0;
		string level;
		String source = Godot.ProjectSettings.GlobalizePath("res://");
		string path = source + "Code/1-1.txt";
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

	private void CreateGoomba(GoombaEnemy goombaEnemy, Vector2I coordsI)
	{
		Vector2 coords = MapToLocal(coordsI);
		goombaEnemy.Position = coords;
		GetParent().AddChild(goombaEnemy);       
	}

	private void CreatePlayer(Player player, Vector2I coordsI)
	{
		Vector2 coords = MapToLocal(coordsI);
		player.Position = coords;
		GetParent().AddChild(player);
	}

	private void PutCell(Vector2I coords, Vector2I identifier)
	{
		SetCell(0, coords, 0, identifier);
	}

	private void InitTileset()
	{
		atlasSource.Texture = tiles;
		tileset.AddSource(atlasSource);
		tileset.AddPhysicsLayer();
		tileset.SetPhysicsLayerCollisionLayer(0, 1);
		tileset.AddPhysicsLayer();
		tileset.SetPhysicsLayerCollisionLayer(1, 2);
		tileset.AddCustomDataLayer();
		tileset.SetCustomDataLayerName(0, "type");
        tileset.AddCustomDataLayer();
        tileset.SetCustomDataLayerName(1, "content");
        tileset.SetCustomDataLayerType(0, Variant.Type.String);
		tileset.TileSize = new Vector2I(16, 16);
		CreatePhysics(new Vector2I(0, 0), "box");
		CreatePhysics(new Vector2I(1, 0), "floor");
		CreatePhysics(new Vector2I(2, 0), "brick");
        CreatePhysics(new Vector2I(3, 0), "boxempty");
		CreatePhysics(new Vector2I(4, 0), "mushroom");

        TileSet = tileset;
	}

	private void CreatePhysics(Vector2I coords, String custom)
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
		tiledat.AddCollisionPolygon(1);
		tiledat.SetCollisionPolygonPoints(1, 0, tileCollision);
		tiledat.SetCustomData("type", custom);
	}

	public async void MoveTile(Vector2 post)
	{
		Vector2I positionMap = LocalToMap(post);
		Vector2I erasecell = new(-1, -1);
		TileData tile = GetCellTileData(0, positionMap);
		if (tile != null)
		{
			var type = tile.GetCustomData("type");
			if (type.AsString() == "box")
			{
				SetCell(0, positionMap, 0, erasecell);
				Sprite2D sprite = new()
				{
					Position = MapToLocal(positionMap),
					Texture = box
				};
				AddChild(sprite);
				Tween tween = CreateTween();
				Vector2 new_position = sprite.Position - new Vector2(0, 8);
				tween.TweenProperty(sprite, "position", new_position, 0.1);
				tween.Chain().TweenProperty(sprite, "position", sprite.Position, 0.1);
				await ToSignal(tween, "finished");
				sprite.QueueFree();
				SetCell(0, positionMap, 0, new Vector2I(3, 0));
				return;
			}
			else if(type.AsString() == "mushroom")
			{
                SetCell(0, positionMap, 0, erasecell);
                Sprite2D sprite = new()
                {
                    Position = MapToLocal(positionMap),
                    Texture = box
                };
                AddChild(sprite);
                Tween tween = CreateTween();
                Vector2 new_position = sprite.Position - new Vector2(0, 8);
                tween.TweenProperty(sprite, "position", new_position, 0.1);
                tween.Chain().TweenProperty(sprite, "position", sprite.Position, 0.1);
                await ToSignal(tween, "finished");
                sprite.QueueFree();
                SetCell(0, positionMap, 0, new Vector2I(3, 0));
                Mushroom mushroom = new();
                mushroom.Position = MapToLocal(positionMap) + new Vector2(0, -16);
				await ToSignal(GetTree().CreateTimer(0.05), "timeout");
                GetParent().AddChild(mushroom);
				return;

            }
			else if (type.AsString() == "brick")
			{
				SetCell(0, positionMap, 0, erasecell);
				Sprite2D sprite = new()
				{
					Position = MapToLocal(positionMap),
					Texture = brick
				};
				AddChild(sprite);
				Tween tween = CreateTween();
				Vector2 new_position = sprite.Position - new Vector2(0, 8);
				tween.TweenProperty(sprite, "position", new_position, 0.1);
				tween.Chain().TweenProperty(sprite, "position", sprite.Position, 0.1);
				await ToSignal(tween, "finished");
				sprite.QueueFree();
				SetCell(0, positionMap, 0, new Vector2I(2, 0));
				return;
			}
		}
	}
}

public partial class Mushroom : CharacterBody2D
{
	private Texture2D mushroom = ResourceLoader.Load<Texture2D>("res://Sprites/Mushroom.png");

	private Area2D area = new();
	private CollisionShape2D areaCollision = new();
	private RectangleShape2D areaShape = new();

	private Sprite2D sprite = new();
	private CollisionShape2D collisionCharacter = new();
	private RectangleShape2D collisionShape = new();

	public override void _Ready()
	{
		sprite.Texture = mushroom;
		collisionShape.Size = new Vector2(8, 15);
		collisionCharacter.Shape = collisionShape;

		areaShape.Size = new Vector2(16, 16);
		areaCollision.Shape = areaShape;
		area.AddChild(areaCollision);

		area.CollisionLayer = 1;
		area.CollisionMask = 1;
		area.BodyEntered += Area_BodyEntered;

		AddChild(collisionCharacter);
		AddChild(area);
		AddChild(sprite);
	}

	private void Area_BodyEntered(Node2D body)
	{
		if (body is Player)
		{
			Player player = body as Player;
			player.Mushroom();
			QueueFree();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsOnFloor())
		{
			Velocity += new Vector2(0, 20);
		}
		Velocity = Velocity with { X = 50 };
		MoveAndSlide();
	}
}
