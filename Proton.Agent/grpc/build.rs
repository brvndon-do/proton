fn main() -> Result<(), Box<dyn std::error::Error>> {
    tonic_prost_build::compile_protos("protos/greeter.proto")?;
    tonic_prost_build::compile_protos("protos/market_data.proto")?;
    Ok(())
}
