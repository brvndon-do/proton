use anyhow::Ok;
use rustyline::ExternalPrinter;
use std::sync::Arc;
use tokio::sync::Mutex;

use crate::{
    cli::{
        commands::{Command, dispatch, parse_command},
        input, output,
    },
    state::AppState,
};

pub async fn run(state: Arc<Mutex<AppState>>) -> anyhow::Result<()> {
    let (mut rx, printer) = input::spawn_reader();

    while let Some(line) = rx.recv().await {
        let input = line.trim();
        if input.is_empty() {
            output::print_info("No input entered.");
            continue;
        }

        match parse_command(input) {
            Some(Command::Quit) => break,
            Some(cmd) => {
                let mut guard = state.lock().await;
                if let Err(e) = dispatch(cmd, &mut guard).await {
                    output::print_error(&e.to_string());
                }
            }
            None => {
                if input.starts_with('/') {
                    output::print_error(&format!("Unknown command: {input}"));
                } else {
                    output::print_error("TODO: send non-commands to LLM");
                }
            }
        }
    }

    output::print_info("Goodbye.");
    Ok(())
}
