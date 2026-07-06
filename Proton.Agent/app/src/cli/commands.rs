use anyhow::Ok;

use crate::state::AppState;

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

pub async fn dispatch(cmd: Command, state: &mut AppState) -> anyhow::Result<()> {
    println!("TODO!");
    Ok(())
}
