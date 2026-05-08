using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_role_at_time = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    event_type = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    target_entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    target_entity_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    before_state_json = table.Column<string>(type: "jsonb", nullable: true),
                    after_state_json = table.Column<string>(type: "jsonb", nullable: true),
                    reason_for_change = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    source_ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    client_info = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    event_payload_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    previous_event_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_number = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    initials = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    year_of_birth = table.Column<int>(type: "integer", nullable: false),
                    sex = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    enrolment_status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    screening_date = table.Column<DateOnly>(type: "date", nullable: true),
                    enrolment_date = table.Column<DateOnly>(type: "date", nullable: true),
                    withdrawal_date = table.Column<DateOnly>(type: "date", nullable: true),
                    withdrawal_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    scope_study_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scope_site_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scope_kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    assigned_by_signature_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_assignments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "signature_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    signer_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    signed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    meaning = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    target_entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    target_entity_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    target_version_or_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    reason_statement = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    authentication_method = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    signature_payload_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    previous_signature_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    client_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    mfa_method = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    continuous_session = table.Column<bool>(type: "boolean", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    signing_key_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_signature_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    study_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    country = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    principal_investigator_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activation_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sites", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "studies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    protocol_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phase = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    therapeutic_area = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sponsor_organization = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    planned_enrolment = table.Column<int>(type: "integer", nullable: false),
                    planned_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    planned_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_studies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    display_title = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    mfa_enrolled = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_login_count = table.Column<int>(type: "integer", nullable: false),
                    password_last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_actor_user_id",
                table: "audit_events",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_event_type",
                table: "audit_events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_occurred_at",
                table: "audit_events",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_previous_event_hash",
                table: "audit_events",
                column: "previous_event_hash");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_target_entity_type_target_entity_id",
                table: "audit_events",
                columns: new[] { "target_entity_type", "target_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_participants_enrolment_status",
                table: "participants",
                column: "enrolment_status");

            migrationBuilder.CreateIndex(
                name: "ix_participants_is_deleted",
                table: "participants",
                column: "is_deleted",
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_participants_site_id",
                table: "participants",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "ix_participants_site_id_subject_number",
                table: "participants",
                columns: new[] { "site_id", "subject_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_assignments_ended_at",
                table: "role_assignments",
                column: "ended_at");

            migrationBuilder.CreateIndex(
                name: "ix_role_assignments_is_deleted",
                table: "role_assignments",
                column: "is_deleted",
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_role_assignments_role",
                table: "role_assignments",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "ix_role_assignments_user_id",
                table: "role_assignments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_signature_records_meaning",
                table: "signature_records",
                column: "meaning");

            migrationBuilder.CreateIndex(
                name: "ix_signature_records_previous_signature_hash",
                table: "signature_records",
                column: "previous_signature_hash");

            migrationBuilder.CreateIndex(
                name: "ix_signature_records_signed_at",
                table: "signature_records",
                column: "signed_at");

            migrationBuilder.CreateIndex(
                name: "ix_signature_records_signer_user_id",
                table: "signature_records",
                column: "signer_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_signature_records_target_entity_type_target_entity_id",
                table: "signature_records",
                columns: new[] { "target_entity_type", "target_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sites_is_deleted",
                table: "sites",
                column: "is_deleted",
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_sites_principal_investigator_user_id",
                table: "sites",
                column: "principal_investigator_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_sites_status",
                table: "sites",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_sites_study_id",
                table: "sites",
                column: "study_id");

            migrationBuilder.CreateIndex(
                name: "ix_sites_study_id_site_number",
                table: "sites",
                columns: new[] { "study_id", "site_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_studies_is_deleted",
                table: "studies",
                column: "is_deleted",
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_studies_protocol_number",
                table: "studies",
                column: "protocol_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_studies_status",
                table: "studies",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_is_deleted",
                table: "users",
                column: "is_deleted",
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_users_status",
                table: "users",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_events");

            migrationBuilder.DropTable(
                name: "participants");

            migrationBuilder.DropTable(
                name: "role_assignments");

            migrationBuilder.DropTable(
                name: "signature_records");

            migrationBuilder.DropTable(
                name: "sites");

            migrationBuilder.DropTable(
                name: "studies");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}