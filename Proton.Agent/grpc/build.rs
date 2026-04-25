// TODO: this is from the example page; it mentions that there are other ways to do it -- look into: https://github.com/hyperium/tonic/blob/master/tonic-build/README.md
fn main() -> Result<(), Box<dyn std::error::Error>> {
    tonic_prost_build::compile_protos("protos/greeter.proto")?;
    Ok(())
}
