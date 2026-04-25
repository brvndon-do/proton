use proton_agent_grpc::grpc_client::ProtonGrpcClient;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut client = ProtonGrpcClient::connect("http://localhost:5182").await?;

    let mut stream = client
        .stream_market_data(vec!["TSLA".into()], vec![])
        .await?;

    while let Some(snapshot) = stream.message().await? {
        println!("{}: close {}", snapshot.symbol, snapshot.close);
    }

    Ok(())
}
