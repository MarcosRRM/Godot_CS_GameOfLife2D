using Godot;
using System.Threading;
using System.Threading.Tasks;

public partial class CellsGrid : ColorRect {

  #region Properties
  private bool[,] Cells { get; set; } = new bool[0, 0];
  private uint AliveCellsCount { get; set; } = 0;
  private (ushort x, ushort y) CurrentGridSize { get; set; } = (0, 0);
  private double CurrentGenerationStep { get; set; } = 0;

  #region NodeReferences
  private SpinBox GridSizeHNode { get; set; }
  private SpinBox GridSizeVNode { get; set; }
  private SpinBox InitialCountInputNode { get; set; }
  private Label ThrottlerValueNode { get; set; }
  private ColorPickerButton AliveColorPickerNode { get; set; }
  private ColorPickerButton DeadColorPickerNode { get; set; }
  private Label FPSValueNode { get; set; }
  #endregion

  #endregion

  #region Node Lifecycle
  public override void _Ready() {

    GD.Randomize();

    #region Initialize Node References
    GridSizeHNode = GetNode<SpinBox>("%GridSizeH");
    GridSizeVNode = GetNode<SpinBox>("%GridSizeV");
    InitialCountInputNode = GetNode<SpinBox>("%InitialCountInput");
    ThrottlerValueNode = GetNode<Label>("%ThrottlerValue");
    AliveColorPickerNode = GetNode<ColorPickerButton>("%AliveColorPicker");
    DeadColorPickerNode = GetNode<ColorPickerButton>("%DeadColorPicker");
    FPSValueNode = GetNode<Label>("%FPSValue");
    #endregion

    CurrentGridSize = ((ushort)GridSizeHNode.Value, (ushort)GridSizeVNode.Value);

    #region SetUp UI Elements
    GridSizeHNode.MaxValue = GridSizeVNode.MaxValue = ushort.MaxValue;
    InitialCountInputNode.MaxValue = CurrentGridSize.x * CurrentGridSize.y;
    Color = DeadColorPickerNode.Color;
    #endregion

    StartNewGeneration();
  }
  public override void _Process(double delta) {
    FPSValueNode.Text = Engine.GetFramesPerSecond().ToString();
  }
  public override void _PhysicsProcess(double delta) {
    CalcNextStep();
    QueueRedraw();
  }
  public override void _Draw() {
    uint drawnAliveCells = 0;
    var viewportSize = GetViewport().GetVisibleRect().Size;

    var cellSize = (x: viewportSize.X / CurrentGridSize.x, y: viewportSize.Y / CurrentGridSize.y);

    for (var y = 0; y < CurrentGridSize.y; y++) {
      for (var x = 0; x < CurrentGridSize.x; x++) {
        if (Cells[y, x]) {
          drawnAliveCells++;
          DrawRect(
            new Rect2(
              x * cellSize.x, y * cellSize.y,
              cellSize.x, cellSize.y
            ),
            AliveColorPickerNode.Color
          );
          if (drawnAliveCells == AliveCellsCount)
            return;
        }
      }
    }
  }
  #endregion

  #region Public functions
  public void StartNewGeneration() {

    CurrentGridSize = ((ushort)GridSizeHNode.Value, (ushort)GridSizeVNode.Value);
    CurrentGenerationStep = 0;

    var newCellsGrid = CreateDeadGrid();

    uint newAliveCellsCount = 0;
    while (newAliveCellsCount < (uint)InitialCountInputNode.Value) {
      (uint x, uint y) newCell = ((uint)GD.RandRange(0, CurrentGridSize.x - 1), (uint)GD.RandRange(0, CurrentGridSize.y - 1));
      if (!newCellsGrid[newCell.y, newCell.x])
        newCellsGrid[newCell.y, newCell.x] = true;
      newAliveCellsCount += 1;
    }

    Cells = newCellsGrid;
    AliveCellsCount = newAliveCellsCount;
    QueueRedraw();
  }
  public void RecalculateMaxAliveCellsInput(float _) {
    var newMaxValue = GridSizeHNode.Value * GridSizeVNode.Value;
    if (InitialCountInputNode.Value > newMaxValue)
      InitialCountInputNode.Value = newMaxValue;
    InitialCountInputNode.MaxValue = newMaxValue;
  }
  public void SetSimulationSpeed(float value) {
    ThrottlerValueNode.Text = $"{(int)value} per second";
    Engine.PhysicsTicksPerSecond = (int)value;
  }
  #endregion

  #region Private functions
  private bool[,] CreateDeadGrid() {
    var deadGrid = new bool[CurrentGridSize.y, CurrentGridSize.x];

    for (ushort y = 0; y < CurrentGridSize.y; y++)
      for (ushort x = 0; x < CurrentGridSize.x; x++)
        deadGrid[y, x] = false;

    return deadGrid;
  }
  private void CalcNextStep() {
    CurrentGenerationStep++;
    uint nextStepAliveCells = 0;
    var nextStepCells = CreateDeadGrid();

    //iterate each row in separeted threads
    Parallel.For(0, CurrentGridSize.y, new ParallelOptions { MaxDegreeOfParallelism = System.Environment.ProcessorCount },
      y => {
        for (ushort x = 0; x < CurrentGridSize.x; x++) {
          var aliveNeighbors = CountAliveNeighbors(x, (uint)y);
          if (
              (Cells[y, x] && (aliveNeighbors == 2 || aliveNeighbors == 3))
            || (!Cells[y, x] && aliveNeighbors == 3)
          ) {
            nextStepCells[y, x] = true;
            Interlocked.Increment(ref nextStepAliveCells);
          }
        }
      }
    );

    Cells = nextStepCells;
    AliveCellsCount = nextStepAliveCells;
  }
  private uint CountAliveNeighbors(uint x, uint y) {
    uint aliveNeighbors = 0;
    for (ushort ny = (ushort)(y - 1); ny <= y + 1; ny++) {
      if (ny < 0 || ny >= CurrentGridSize.y) continue;
      for (ushort nx = (ushort)(x - 1); nx <= x + 1; nx++) {
        if (
             (nx != x || ny != y)                              //Not the cell itself
          && (nx >= 0 && ny >= 0)                              //Not before the top and left borders
          && (nx < CurrentGridSize.x && ny < CurrentGridSize.y)//Not before the bottom and right borders
          && (Cells[ny, nx])                                   //Neighbor is alive
        )
          aliveNeighbors++;
      }
    }
    return aliveNeighbors;
  }
  #endregion
}
