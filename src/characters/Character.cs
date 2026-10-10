using System;
using Godot;

namespace Characters;

public partial class Character : CharacterBody2D
{
  public const float Deadzone = 0.1f;
  public const float Speed = 64f;
  public const int GridSize = 16;
  public CharacterBody2D? chara;
  [Export]
  public AnimatedSprite2D? anim;

  private RayCast2D? _ray;
  private RayCast2D GetRay()
  {
	return _ray ?? throw new Exception("No raycast");
  }

  private PlayerState _playerState = PlayerState.Idle;

  public const float TimeBeforeMoving = 0.15f;

  private Direction _currentDirection = Direction.Down;
  private Direction CurrentDirection
  {
	get => _currentDirection;
	set
	{
	  _currentDirection = value;
	  GetRay().RotationDegrees = value switch
	  {
		Direction.Up => 0,
		Direction.Left => 90,
		Direction.Right => -90,
		_ => -180,
	  };
	}
  }

  private double _holdingInputFor = 0;
  private bool _wasPressingInput = false;

  private Vector2 _originPosition;
  private Vector2 _destination;
  private float _progression;

  public override void _Ready()
  {
	_ray = GetNode<RayCast2D>("RayCast2D");
  }

  private void MoveTo(Direction direction)
  {
	_originPosition = CurrentCharacterTile();
	_destination = CurrentCharacterTile() + DirectionToVector(direction) * GridSize;
	_destination = new Vector2(
	  Mathf.Round(_destination.X / 16) * 16,
	  Mathf.Round(_destination.Y / 16) * 16
	);
	_progression = 0;
	_playerState = PlayerState.Moving;
  }
  private void StopMoving()
  {
	_holdingInputFor = 0;
	_playerState = PlayerState.Idle;
  }

  public override void _Process(double delta)
  {
	PlayAnimation();
  }

  public void PlayAnimation()
  {
	if (anim == null) return;

	anim.FlipH = _currentDirection == Direction.Right;
	anim.Animation = _playerState switch
	{
	  PlayerState.Moving => _currentDirection switch
	  {
		Direction.Up => "walk_up",
		Direction.Down => "walk_down",
		_ => "walk_left",
	  },
	  _ => _currentDirection switch
	  {
		Direction.Up => "idle_up",
		Direction.Down => "idle_down",
		_ => "idle_left",
	  }
	};
  }

  public override void _PhysicsProcess(double delta)
  {
	// The process of moving should be:
	// 0. Wait for player input.
	// 1. Player presses input.
	// 2. We start counting how long the input is down.
	// 3. If button is released before a set amount of time, change char orientation then stop and go back to 0.
	// 4. Else, start moving the char toward the next tile in the pressed direction.
	// 5. If input is still pressed after initial movement, go back to step 4.

	Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

	var isInputPressed = Mathf.Abs(direction.X) >= Deadzone || Mathf.Abs(direction.Y) >= Deadzone;

	if (_playerState == PlayerState.Moving)
	{
	  _progression += (float)delta * Speed / GridSize;
	  // Position = _originPosition.Lerp(_destination, Mathf.Min(_progression, 1));
	  MoveAndCollide(_originPosition.Lerp(_destination, Mathf.Min(_progression, 1)) - Position);
	  if (_progression >= 1)
	  {
		if (isInputPressed)
		{
		  if (isInputPressed)
			_currentDirection = InputToDirection();
		  MoveTo(_currentDirection);
		  return;
		}
		StopMoving();
	  }
	  return;
	}

	if (isInputPressed)
	{
	  var newDirection = InputToDirection();
	  if (!_wasPressingInput && newDirection == _currentDirection)
	  {
		MoveTo(_currentDirection);
		return;
	  }

	  _wasPressingInput = true;
	  _currentDirection = newDirection;

	  if (_holdingInputFor >= TimeBeforeMoving)
	  {
		MoveTo(_currentDirection);
		return;
	  }

	  _holdingInputFor += delta;
	  return;
	}

	_wasPressingInput = false;
	_holdingInputFor = 0;
  }

  public static Direction InputToDirection()
  {
	Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
	return InputToDirection(direction);
  }

  public static Direction InputToDirection(Vector2 input)
  {
	if (Mathf.Abs(input.X) > Mathf.Abs(input.Y))
	{
	  return Mathf.Sign(input.X) > 0 ? Direction.Right : Direction.Left;
	}
	return Mathf.Sign(input.Y) > 0 ? Direction.Down : Direction.Up;
  }

  public static Vector2I DirectionToVector(Direction direction) => direction switch
  {
	Direction.Up => Vector2I.Up,
	Direction.Down => Vector2I.Down,
	Direction.Left => Vector2I.Left,
	Direction.Right => Vector2I.Right,
	_ => Vector2I.Up // Used for exhaustivity only, should never be called.
  };

  public Vector2I CurrentCharacterTile() => new(
  Mathf.FloorToInt(Position.X),
  Mathf.FloorToInt(Position.Y)
  );
}

public enum Direction
{
  Up,
  Down,
  Left,
  Right
}

public enum PlayerState
{
  Idle,
  Moving
}
