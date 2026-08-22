mod cli;
mod market;
mod state;

use anyhow::Result;
use clap::Parser;
use state::AppState;
use std::sync::Arc;
use tokio::sync::Mutex;

use crate::cli::output;
use crate::cli::repl;

#[derive(clap::Parser)]
struct Args {
    #[arg(long, default_value = "http://localhost:5000")]
    host: String,
}

#[tokio::main]
async fn main() -> Result<()> {
    let args = Args::parse();
    let state = Arc::new(Mutex::new(AppState::new(&args.host)));

    output::print_banner(&args.host, false);
    repl::run(state).await?;

    Ok(())
}
