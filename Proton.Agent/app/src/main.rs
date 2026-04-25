use proton_agent_grpc::grpc_client::connect_and_run;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let response = connect_and_run().await?;

    println!("Server replied: {}", response.message);

    Ok(())
}
