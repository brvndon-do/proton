use std::collections::HashMap;

use anyhow::{Context, Ok, Result};
use proton_agent_grpc::grpc_client::{ProtonGrpcClient, proton_market_data::MarketSnapshot};

pub struct AppState {
    grpc_client: Option<ProtonGrpcClient>,
    engine_url: String,
    active_symbols: HashMap<String, bool>,
}

impl AppState {
    pub fn new(url: &str) -> Self {
        Self {
            grpc_client: None,
            engine_url: url.to_string(),
            active_symbols: HashMap::new(),
        }
    }

    pub async fn connect(&mut self) -> Result<()> {
        let client = ProtonGrpcClient::connect(&self.engine_url).await?;
        self.grpc_client = Some(client);

        Ok(())
    }

    pub async fn subscribe_symbol(&mut self, symbol: String) -> Result<()> {
        let client = self
            .grpc_client
            .as_mut()
            .context("not connected; invoke /start first")?;

        let mut stream = client
            .stream_market_data(vec![symbol.clone()], vec![])
            .await?;

        self.active_symbols.insert(symbol, true);

        // TODO: this is just for prototyping; this method actually never exits and will hang the main thread
        while let Some(snapshot) = stream.message().await? {
            println!("{}: close {}", snapshot.symbol, snapshot.close);
        }

        Ok(())
    }

    pub fn is_connected(&self) -> bool {
        self.grpc_client.is_some()
    }
}
