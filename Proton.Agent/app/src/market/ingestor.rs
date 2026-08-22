use proton_agent_grpc::grpc_client::{ProtonGrpcClient, proton_market_data::MarketSnapshot};
use std::collections::HashMap;
use tokio::{
    sync::{broadcast, mpsc},
    task::JoinHandle,
};

use crate::market::message::IngestorCommand;

#[derive(Clone)]
pub struct IngestorHandle {
    commands: mpsc::Sender<IngestorCommand>,
    events: broadcast::Sender<MarketSnapshot>,
}

impl IngestorHandle {
    // TODO: -> Result<()>; for now, leave off because rustc will complain
    pub async fn subscribe(&self, symbol: String) {
        todo!()
    }

    pub fn events(&self) -> broadcast::Receiver<MarketSnapshot> {
        todo!()
    }
}

pub struct Ingestor {
    client: ProtonGrpcClient,
    streams: HashMap<String, JoinHandle<()>>,
    events: broadcast::Sender<MarketSnapshot>,
}

impl Ingestor {
    pub async fn run(mut self, mut commands: mpsc::Receiver<IngestorCommand>) {
        while let Some(cmd) = commands.recv().await {
            match cmd {
                IngestorCommand::Subscribe(symbol) => {
                    if self.streams.contains_key(&symbol) {
                        continue;
                    }

                    match self.open_stream(&symbol).await {
                        Ok(handle) => self.streams.insert(symbol, handle),
                        Err(e) => {
                            todo!();
                        }
                    }
                }
                IngestorCommand::Unsubscribe(symbol) => {
                    if let Some(handle) = self.streams.remove(&symbol) {
                        handle.abort();
                    }
                }
            }

            for (_, handle) in self.streams.drain() {
                handle.abort();
            }
        }
    }

    async fn open_stream(&mut self, symbol: &str) -> Result<JoinHandle<()>> {
        todo!("grpc: open stream, pump task forwarding into self.events")
    }
}
