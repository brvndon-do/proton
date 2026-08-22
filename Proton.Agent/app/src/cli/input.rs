use rustyline::{DefaultEditor, ExternalPrinter, error::ReadlineError};
use tokio::sync::mpsc;

pub fn spawn_reader() -> (mpsc::Receiver<String>, impl ExternalPrinter + Send) {
    let mut rl = DefaultEditor::new().expect("editor");
    let printer = rl.create_external_printer().expect("printer");
    let (tx, rx) = mpsc::channel(1);

    std::thread::spawn(move || {
        loop {
            match rl.readline("> ") {
                Ok(line) => {
                    if tx.blocking_send(line).is_err() {
                        break;
                    }
                }
                Err(ReadlineError::Interrupted) => continue, // ctrl+c: new prompt
                Err(ReadlineError::Eof) => break,            // ctrl+d: quit
                Err(_) => break,
            }
        }
    });

    (rx, printer)
}
