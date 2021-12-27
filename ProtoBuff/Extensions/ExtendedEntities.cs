namespace SC2APIProtocol
{
    using pb = global::Google.Protobuf;

    /// <summary>
    /// Adds a new constructor with setable coordinates
    /// </summary>
    public sealed partial class Point2D : pb::IMessage<Point2D>
    {
        // Custom add
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public Point2D(float x, float y) : this()
        {
            OnConstruction();
            X = x;
            Y = y;
        }
    }
}
