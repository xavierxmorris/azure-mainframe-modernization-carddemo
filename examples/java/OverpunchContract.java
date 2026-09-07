import java.io.IOException;
import java.math.BigDecimal;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;

public final class OverpunchContract {
    private static final String POSITIVE = "{ABCDEFGHI";
    private static final String NEGATIVE = "}JKLMNOPQR";

    private OverpunchContract() {}

    public static BigDecimal decode(String field, int scale) {
        if (scale < 0 || scale > 3) {
            throw new IllegalArgumentException("supported scales are 0 through 3");
        }
        String raw = field.strip();
        if (raw.isEmpty()) {
            throw new NumberFormatException("a signed COBOL decimal field cannot be blank");
        }
        char last = raw.charAt(raw.length() - 1);
        int positive = POSITIVE.indexOf(last);
        int negative = NEGATIVE.indexOf(last);
        char digit = positive >= 0 ? (char) ('0' + positive)
            : negative >= 0 ? (char) ('0' + negative) : last;
        String digits = raw.substring(0, raw.length() - 1) + digit;
        for (int i = 0; i < digits.length(); i++) {
            if (digits.charAt(i) < '0' || digits.charAt(i) > '9') {
                throw new NumberFormatException("expected ASCII digits and a trailing overpunch");
            }
        }
        // Match FixedWidth.SignedDecimal's Int64 magnitude boundary, then apply the scale.
        long magnitude = Long.parseLong(digits);
        return BigDecimal.valueOf(negative >= 0 ? -magnitude : magnitude, scale);
    }

    public static void main(String[] args) throws IOException {
        if (args.length != 1) {
            throw new IllegalArgumentException("usage: OverpunchContract <signed-overpunch.txt>");
        }
        int count = 0;
        for (String line : Files.readAllLines(Path.of(args[0]), StandardCharsets.UTF_8)) {
            if (line.isEmpty() || line.startsWith("#")) {
                continue;
            }
            String[] columns = line.split("\\|", -1);
            if (columns.length != 3) {
                throw new IllegalArgumentException("contract rows must have exactly three columns");
            }
            int scale = Integer.parseInt(columns[1]);
            String actual;
            try {
                actual = decode(columns[0], scale).toPlainString();
            } catch (IllegalArgumentException rejected) {
                actual = "ERROR";
            }
            count++;
            if (!columns[2].equals(actual)) {
                throw new AssertionError("fixture " + count + ": expected " + columns[2] + ", got " + actual);
            }
        }
        if (count == 0) {
            throw new IllegalArgumentException("the contract contains no fixtures");
        }
        System.out.println("PASS: " + count + " shared signed-overpunch fixtures.");
    }
}
