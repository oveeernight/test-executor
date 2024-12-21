export SRC_DIR=.
export DST_DIR=TestExecutor.Core/Models

protoc -I=$SRC_DIR --csharp_out=$DST_DIR $SRC_DIR/test-expressions.proto
