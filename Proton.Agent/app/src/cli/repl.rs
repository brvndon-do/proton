use anyhow::Ok;
use std::{
    io::{self, Write},
    sync::Arc,
};
use tokio::sync::Mutex;

use crate::{
    cli::{
        commands::{Command, dispatch, parse_command},
        output,
    },
    state::AppState,
};

pub async fn run(state: Arc<Mutex<AppState>>) -> anyhow::Result<()> {
    loop {
        print!("> ");
        io::stdout().flush()?;

        let mut input = String::new();
        let bytes = io::stdin().read_line(&mut input)?;

        if bytes == 0 {
            println!();
            break;
        }

        let input = input.trim();
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
