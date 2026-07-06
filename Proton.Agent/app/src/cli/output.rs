use console::Style;

pub fn print_banner(engine_url: &str, is_connected: bool) {
    println!(
        "
______          _
| ___ \\        | |
| |_/ / __ ___ | |_ ___  _ __
|  __/ '__/ _ \\| __/ _ \\| '_ \\
| |  | | | (_) | || (_) | | | |
\\_|  |_|  \\___/ \\__\\___/|_| |_|
        "
    );
    let style = Style::new().bold();
    println!("Engine URL: {}", style.apply_to(engine_url));

    let connection_msg = if is_connected {
        "CONNECTED"
    } else {
        "DISCONNECTED"
    };
    println!("Connection: {}", style.apply_to(connection_msg));
}

pub fn print_success(msg: &str) {
    let style = Style::new().bold().green();
    println!("{}: {}", style.apply_to("[SUCCESS]"), msg);
}

pub fn print_info(msg: &str) {
    let style = Style::new().bold().true_color(173, 216, 230);
    println!("{}: {}", style.apply_to("[INFO]"), msg);
}

pub fn print_error(msg: &str) {
    let style = Style::new().bold().red();
    println!("{}: {}", style.apply_to("[ERROR]"), msg);
}

// TODO: table styles
