export type DetailFieldLabel = "title" | "issueType" | "status" | "issueKey" | "issueLink";

export type TextDetailField = { kind: "text"; label: DetailFieldLabel; value: string };
export type DateDetailField = { kind: "date"; label: DetailFieldLabel; value: Date };
export type LinkDetailField = { kind: "link"; label: DetailFieldLabel; text: string; url: string };
export type ChipDetailField = { kind: "chip"; label: DetailFieldLabel; value: string };

export type DetailField = TextDetailField | DateDetailField | LinkDetailField | ChipDetailField;

export type ConnectorDetailGroup = {
    connectorKey: string;
    fields: DetailField[];
};

export type DetailsContract = {
    groups: ConnectorDetailGroup[];
};
