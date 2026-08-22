use anyhow::{Ok, Result};

use crate::{cli::output, state::AppState};

#[derive(Debug)]
pub enum Command {
    Start,
    Stop,
    Symbol { action: String, args: Vec<String> },
    Status,
    Quit,
}

pub fn parse_command(input: &str) -> Option<Command> {
    let tokens: Vec<&str> = input.split_whitespace().collect();

    match tokens.as_slice() {
        ["/start"] => Some(Command::Start),
        ["/stop"] => Some(Command::Stop),
        ["/symbol", action, args @ ..] => Some(Command::Symbol {
            action: action.to_string(),
            args: args.iter().map(|s| s.to_string()).collect(),
        }),
        ["/status"] => Some(Command::Status),
        ["/quit"] => Some(Command::Quit),
        _ => None,
    }
}

pub async fn dispatch(cmd: Command, state: &mut AppState) -> Result<()> {
    match cmd {
        Command::Start => {
            state.connect().await?;
            output::print_success("Connection successful.");
        }
        Command::Symbol { action, args } => {
            if !state.is_connected() {
                output::print_info("Engine must be connected first. Invoke /start.");
                return Ok(());
            }

            // TODO: proper argument handling?
            if args.len() > 1 {
                output::print_error("Too many arguments!");
                return Ok(());
            }

            if action == "add" {
                // add to some sort of producer/consumer channel
            }
        }
        _ => println!("TODO: finish all command branches"),
    }

    Ok(())
}
