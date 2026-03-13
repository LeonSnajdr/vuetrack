import "vuetify/labs/rules";

declare module "vuetify/labs/rules" {
    interface RuleAliases {
        dateAfter: (startTime: Date | null | undefined, err?: string) => (value: Date | null | undefined) => boolean | string;
    }
}
