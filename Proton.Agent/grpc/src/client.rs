pub mod proton_market_data {
    tonic::include_proto!("proton.market_data");
}

use proton_market_data::market_data_client::MarketDataClient;
use proton_market_data::{MarketSnapshot, MarketSnapshotRequest};
use tonic::transport::Channel;

pub struct ProtonGrpcClient {
    client: MarketDataClient<Channel>,
}

impl ProtonGrpcClient {
    pub async fn connect(uri: &str) -> Result<Self, Box<dyn std::error::Error>> {
        let client = MarketDataClient::connect(uri.to_string()).await?;

        Ok(Self { client })
    }

    pub async fn stream_market_data(
        &mut self,
        symbols: Vec<String>,
        requested_indicators: Vec<String>,
    ) -> Result<tonic::Streaming<MarketSnapshot>, Box<dyn std::error::Error>> {
        let request = tonic::Request::new(MarketSnapshotRequest {
            symbols,
            indicators: requested_indicators,
        });

        let stream = self
            .client
            .stream_market_snapshot(request)
            .await?
            .into_inner();

        Ok(stream)
    }
}
