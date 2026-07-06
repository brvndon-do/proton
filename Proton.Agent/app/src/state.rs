use anyhow::Result;
use proton_agent_grpc::grpc_client::ProtonGrpcClient;

pub struct AppState {
    grpc_client: Option<ProtonGrpcClient>,
    engine_url: String,
    active_symbols: Vec<String>,
    is_connected: bool,
}

impl AppState {
    pub fn new(url: &str) -> Self {
        Self {
            grpc_client: None,
            engine_url: url.to_string(),
            active_symbols: vec![],
            is_connected: false,
        }
    }

    pub async fn connect(&mut self) -> Result<()> {
        let client = ProtonGrpcClient::connect(&self.engine_url).await?;
        self.grpc_client = Some(client);
        self.is_connected = true;

        Ok(())
    }
}
