module FakeBuilder.Tests
    
    open System
    open System.IO
    open Xunit
    open Fake.FsQuery

    // run tests against fake files
    let fileList = ["foo.txt"
                    "folder\\bar.txt"
                    "folder\\foo.txt"]
    let inventory = FsInventory(".")
    inventory.files <- tupleRelative "" fileList

    (*
    [<Fact>]
    let dummyTest () =
        Assert.Equal<int>(1, 1)
    *)

    [<Fact>]
    let FsQueryAllTest () =
        let files = FsQuery(inventory, []).files("*")
        Assert.Equal<int>(files.Length, fileList.Length)
    
    [<Fact>]
    let FsQueryExcludeTest () =
        let files = FsQuery(inventory, (toPatterns ["*"])).files("*")
        Assert.Equal<int>(files.Length, 0)

    [<Fact>]
    let FsQueryAsterixTest () =
        let files = FsQuery(inventory, []).files("*foo.txt")
        Assert.Equal<int>(files.Length, 2)
    
    [<Fact>]
    let FsQuerySubfolderTest () =
        let files = FsQuery(inventory, []).files("*/foo.txt")
        Assert.Equal<int>(files.Length, 1)
    
    [<Fact>]
    let FsQueryExtTest () =
        let files = FsQuery(inventory, []).files("*.txt")
        Assert.Equal<int>(files.Length, 3)

    [<Fact>]
    let FsQueryExtTest2 () =
        let files = FsQuery(inventory, []).files("folder/*.txt")
        Assert.Equal<int>(files.Length, 2)

