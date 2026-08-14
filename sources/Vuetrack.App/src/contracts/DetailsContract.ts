import type { IntegrationKey } from "@/contracts/IntegrationContract";

export type DetailFieldLabel = "title" | "issueType" | "status" | "issueLink";

export type TextDetailField = { kind: "text"; label: DetailFieldLabel; value: string };
export type LinkDetailField = { kind: "link"; label: DetailFieldLabel; text: string; url: string };
export type ChipDetailField = { kind: "chip"; label: DetailFieldLabel; value: string };

export type DetailField = TextDetailField | LinkDetailField | ChipDetailField;

export type IntegrationDetailGroup = {
    key: IntegrationKey;
    fields: DetailField[];
};

export type DetailsContract = {
    groups: IntegrationDetailGroup[];
};
