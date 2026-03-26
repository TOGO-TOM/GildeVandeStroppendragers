// API Configuration
const API_URL = '/api/members';

let editingMemberId = null;
let editingAddressId = null;
let currentMembers = [];

// Make sure API_URL is globally accessible
window.API_URL = API_URL;

// Helper function to escape HTML and prevent XSS
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Load members on page load
document.addEventListener('DOMContentLoaded', () => {
    // Check authentication first
    const user = checkAuth();
    if (!user) return; // Will redirect to login
    
    // Initialize auth UI (user info, logout button)
    initAuthUI();
    
    // Show page after auth check
    document.body.classList.add('loaded');
    
    loadMembers();
    
    const memberForm = document.getElementById('memberForm');
    if (memberForm) {
        memberForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            await saveMember();
        });
    }

    // Add real-time validation for member number
    const memberNumberInput = document.getElementById('memberNumber');
    if (memberNumberInput) {
        memberNumberInput.addEventListener('blur', async (e) => {
            await checkMemberNumber(e.target.value);
        });
    }

    // File input change handler
    const restoreFileInput = document.getElementById('restoreFile');
    if (restoreFileInput) {
        restoreFileInput.addEventListener('change', (e) => {
            const fileName = e.target.files[0]?.name || '';
            const fileNameEl = document.getElementById('fileName');
            if (fileNameEl) {
                fileNameEl.textContent = fileName ? `Selected: ${fileName}` : '';
            }
        });
    }
});

// Check if member number is already in use
async function checkMemberNumber(memberNumber) {
    if (!memberNumber) return;
    
    const memberId = document.getElementById('memberId').value;
    const messageEl = document.getElementById('memberNumberMessage');
    
    if (!messageEl) return;
    
    try {
        const url = memberId 
            ? `${API_URL}/check-number/${parseInt(memberNumber)}?excludeId=${memberId}`
            : `${API_URL}/check-number/${parseInt(memberNumber)}`;
            
        const response = await fetchWithAuth(url);
        const result = await response.json();
        
        if (result.exists) {
            messageEl.className = 'error-message';
            messageEl.textContent = 'This member number is already in use';
            return false;
        } else {
            messageEl.className = 'success-message';
            messageEl.textContent = 'Member number is available';
            return true;
        }
    } catch (error) {
        console.error('Error checking member number:', error);
        return false;
    }
}

// Load all members
async function loadMembers() {
    console.log('Loading members from:', API_URL);
    
    try {
        const response = await fetchWithAuth(API_URL);
        console.log('Response status:', response.status);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const members = await response.json();
        console.log('Members loaded:', members.length);
        
        // Store members globally for sorting
        currentMembers = members;
        
        displayMembers(members);
        
        // Load custom fields for the form
        await loadCustomFieldsForForm();
    } catch (error) {
        console.error('Error loading members:', error);
        
        const container = document.getElementById('membersList');
        container.innerHTML = `
            <div style="background: #fff; padding: 40px; border-radius: 8px; text-align: center;">
                <div style="font-size: 48px; margin-bottom: 20px;">??</div>
                <h3 style="color: #d32f2f; margin-bottom: 10px;">Failed to Load Members</h3>
                <p style="color: #666; margin-bottom: 20px;">
                    ${error.message === 'Failed to fetch' 
                        ? 'Cannot connect to the API. Make sure the application is running.' 
                        : error.message}
                </p>
                <div style="background: #f5f5f5; padding: 15px; border-radius: 6px; margin-bottom: 20px;">
                    <p style="font-size: 13px; color: #333; margin-bottom: 10px;"><strong>Quick fixes:</strong></p>
                    <ol style="text-align: left; font-size: 13px; color: #666; padding-left: 20px;">
                        <li>Press <strong>F5</strong> in Visual Studio to start the application</li>
                        <li>Or run: <code style="background: #fff; padding: 2px 6px; border-radius: 3px;">dotnet run</code></li>
                        <li>Make sure you're accessing: <code style="background: #fff; padding: 2px 6px; border-radius: 3px;">https://localhost:7223</code></li>
                        <li>Check that SQL Server LocalDB is running</li>
                    </ol>
                </div>
                <button class="btn btn-primary" onclick="location.reload()" style="display: inline-block; margin: 0 auto;">
                    Retry
                </button>
            </div>
        `;
    }
}

// Display members as list
function displayMembers(members) {
    const container = document.getElementById('membersList');

    // Store members for sorting
    currentMembers = members;

    if (members.length === 0) {
        container.innerHTML = `
            <div class="no-members">
                <div style="font-size: 48px; margin-bottom: 16px;">No Members</div>
                <div style="color: #666; font-size: 16px;">No members yet. Add your first member using the form!</div>
            </div>
        `;
        return;
    }

    container.innerHTML = `
        <div class="member-list">
            ${members.map(member => {
                const statusClass = member.isAlive ? 'alive' : 'deceased';
                const statusText = member.isAlive ? 'Alive' : 'Deceased';
                const itemClass = member.isAlive ? '' : 'deceased';
                const initials = (member.firstName[0] + member.lastName[0]).toUpperCase();

                let details = [];
                if (member.email) details.push(member.email);
                if (member.phoneNumber) details.push(member.phoneNumber);
                if (member.address) details.push(`${member.address.city}`);

                return `
                    <div class="member-list-item ${itemClass}" onclick="showContactCard(${member.id})">
                        <div class="member-avatar">${initials}</div>
                        <div class="member-list-info">
                            <div class="member-list-name">${escapeHtml(member.firstName)} ${escapeHtml(member.lastName)}</div>
                            <div class="member-list-details">${details.join(' | ')}</div>
                        </div>
                        <div class="member-list-meta">
                            <div class="member-list-number">${member.memberNumber ? '#' + escapeHtml(member.memberNumber) : 'No #'}</div>
                            <div class="member-list-status ${statusClass}">${statusText}</div>
                        </div>
                    </div>
                `;
            }).join('')}

            ${members.length > 10 ? `
                <div class="member-list-more" onclick="loadMoreMembers()">
                    <span>?? Load More Members</span>
                </div>
            ` : ''}
        </div>
    `;
}

// Sort members function
function sortMembers() {
    const sortBy = document.getElementById('sortBy')?.value;
    if (!sortBy || sortBy === 'default' || !currentMembers || currentMembers.length === 0) {
        return;
    }

    let sortedMembers = [...currentMembers];

    switch(sortBy) {
        case 'lastName-asc':
            sortedMembers.sort((a, b) => (a.lastName || '').localeCompare(b.lastName || ''));
            break;
        case 'lastName-desc':
            sortedMembers.sort((a, b) => (b.lastName || '').localeCompare(a.lastName || ''));
            break;
        case 'memberNumber-asc':
            sortedMembers.sort((a, b) => {
                const aNum = a.memberNumber || 0;
                const bNum = b.memberNumber || 0;
                if (aNum === 0 && bNum === 0) return 0;
                if (aNum === 0) return 1; // Null values go to end
                if (bNum === 0) return -1;
                return aNum - bNum;
            });
            break;
        case 'memberNumber-desc':
            sortedMembers.sort((a, b) => {
                const aNum = a.memberNumber || 0;
                const bNum = b.memberNumber || 0;
                if (aNum === 0 && bNum === 0) return 0;
                if (aNum === 0) return 1; // Null values go to end
                if (bNum === 0) return -1;
                return bNum - aNum;
            });
            break;
    }

    displayMembers(sortedMembers);
}

// Show contact card modal
async function showContactCard(id) {
    try {
        const response = await fetch(`${API_URL}/${id}`);
        if (!response.ok) throw new Error('Failed to load member');
        
        const member = await response.json();
        console.log('Member data loaded:', member); // Debug log
        console.log('Custom field values:', member.customFieldValues); // Debug log
        
        const initials = (member.firstName[0] + member.lastName[0]).toUpperCase();
        const statusClass = member.isAlive ? 'alive' : 'deceased';
        const statusText = member.isAlive ? 'Alive' : 'Deceased';

        // Prepare avatar display (photo or initials)
        let avatarHTML = '';
        if (member.photoBase64) {
            avatarHTML = `<img src="${member.photoBase64}" alt="${escapeHtml(member.firstName)} ${escapeHtml(member.lastName)}" style="width: 100%; height: 100%; object-fit: cover; border-radius: 50%;" />`;
        } else {
            avatarHTML = initials;
        }

        // Calculate seniority
        let seniorityHTML = '';
        if (member.seniorityDate) {
            const seniority = calculateSeniority(member.seniorityDate);
            seniorityHTML = `
                <div class="contact-info-section">
                    <h3>Seniority (Ancienniteit)</h3>
                    <div class="seniority-highlight">
                        <div class="seniority-years">${seniority.years} years, ${seniority.months} months</div>
                        <div class="seniority-label">Member since ${formatDate(member.seniorityDate)}</div>
                    </div>
                </div>
            `;
        }

        const modalContent = `
            <div class="contact-card">
                <div class="contact-card-header">
                    <div class="contact-card-avatar">${avatarHTML}</div>
                    <div class="contact-card-name">${escapeHtml(member.firstName)} ${escapeHtml(member.lastName)}</div>
                    <div class="contact-card-number">${member.memberNumber ? '#' + escapeHtml(member.memberNumber) : 'No Member #'}</div>
                    <div class="contact-card-status ${statusClass}">${statusText}</div>
                </div>

                <div class="contact-card-body">
                    ${seniorityHTML}

                    <div class="contact-info-section">
                        <h3>Personal Information</h3>
                        <div class="contact-info-grid">
                            <div class="contact-info-item">
                                <div class="contact-info-label">Gender</div>
                                <div class="contact-info-value">${escapeHtml(member.gender || 'Not specified')}</div>
                            </div>
                            <div class="contact-info-item">
                                <div class="contact-info-label">Role</div>
                                <div class="contact-info-value">${escapeHtml(member.role || 'Not specified')}</div>
                            </div>
                            ${member.birthDate ? `
                                <div class="contact-info-item">
                                    <div class="contact-info-label">Age</div>
                                    <div class="contact-info-value">${calculateAge(member.birthDate)} years (Born: ${formatDate(member.birthDate)})</div>
                                </div>
                            ` : ''}
                        </div>
                    </div>

                    <div class="contact-info-section">
                        <h3>Contact Information</h3>
                        <div class="contact-info-grid">
                            ${member.email ? `
                                <div class="contact-info-item">
                                    <div class="contact-info-label">Email</div>
                                    <div class="contact-info-value">${escapeHtml(member.email)}</div>
                                </div>
                            ` : ''}
                            ${member.phoneNumber ? `
                                <div class="contact-info-item">
                                    <div class="contact-info-label">Phone Number</div>
                                    <div class="contact-info-value">${escapeHtml(member.phoneNumber)}</div>
                                </div>
                            ` : ''}
                        </div>
                    </div>

                    ${member.address ? `
                        <div class="contact-info-section">
                            <h3>Address</h3>
                            <div class="contact-info-grid">
                                <div class="contact-info-item">
                                    <div class="contact-info-label">Street</div>
                                    <div class="contact-info-value">${escapeHtml(member.address.street)}${member.address.houseNumber ? ' ' + escapeHtml(member.address.houseNumber) : ''}</div>
                                </div>
                                <div class="contact-info-item">
                                    <div class="contact-info-label">City</div>
                                    <div class="contact-info-value">${escapeHtml(member.address.city)}</div>
                                </div>
                                <div class="contact-info-item">
                                    <div class="contact-info-label">Postal Code</div>
                                    <div class="contact-info-value">${escapeHtml(member.address.postalCode)}</div>
                                </div>
                                ${member.address.country ? `
                                    <div class="contact-info-item">
                                        <div class="contact-info-label">Country</div>
                                        <div class="contact-info-value">${escapeHtml(member.address.country)}</div>
                                    </div>
                                ` : ''}
                            </div>
                        </div>
                    ` : ''}

                    ${member.customFieldValues && Array.isArray(member.customFieldValues) && member.customFieldValues.length > 0 ? `
                        <div class="contact-info-section">
                            <h3>Additional Information</h3>
                            <div class="contact-info-grid">
                                ${member.customFieldValues.map(cf => {
                                    if (!cf || !cf.customField) return '';
                                    let displayValue = cf.value || 'Not specified';
                                    if (cf.customField.fieldType === 'Checkbox') {
                                        displayValue = cf.value === 'true' || cf.value === '1' ? 'Yes' : 'No';
                                    } else if (cf.customField.fieldType === 'Date' && cf.value) {
                                        displayValue = formatDate(cf.value);
                                    }
                                    return `
                                        <div class="contact-info-item">
                                            <div class="contact-info-label">${escapeHtml(cf.customField.fieldLabel)}</div>
                                            <div class="contact-info-value">${escapeHtml(displayValue)}</div>
                                        </div>
                                    `;
                                }).join('')}

                            </div>
                        </div>
                    ` : ''}
                </div>

                <div class="contact-card-actions">
                    <button class="btn btn-primary" onclick="editMemberFromCard(${member.id}); closeContactModal();">Edit Member</button>
                    <button class="btn btn-secondary" onclick="deleteMember(${member.id}); closeContactModal();">Delete Member</button>
                </div>
            </div>
        `;
        
        document.getElementById('contactCardContent').innerHTML = modalContent;
        document.getElementById('contactModal').style.display = 'block';
        document.body.style.overflow = 'hidden';
    } catch (error) {
        console.error('Error showing contact card:', error);
        showMessage('Failed to load contact details.', 'error');
    }
}

// Calculate age from birth date
function calculateAge(birthDate) {
    const birth = new Date(birthDate);
    const now = new Date();

    let age = now.getFullYear() - birth.getFullYear();
    const monthDiff = now.getMonth() - birth.getMonth();

    if (monthDiff < 0 || (monthDiff === 0 && now.getDate() < birth.getDate())) {
        age--;
    }

    return age;
}

// Calculate seniority (years and months)
function calculateSeniority(seniorityDate) {
    const start = new Date(seniorityDate);
    const now = new Date();
    
    let years = now.getFullYear() - start.getFullYear();
    let months = now.getMonth() - start.getMonth();
    
    if (months < 0) {
        years--;
        months += 12;
    }
    
    return { years, months };
}

// Format date for display
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' });
}

// Close contact modal
function closeContactModal() {
    document.getElementById('contactModal').style.display = 'none';
    document.body.style.overflow = '';
}

// Edit member from contact card
async function editMemberFromCard(id) {
    await editMember(id);
}

// Filter members by search
function filterMembers() {
    const searchTerm = document.getElementById('searchInput').value.toLowerCase();
    const memberItems = document.querySelectorAll('.member-list-item');
    
    memberItems.forEach(item => {
        const text = item.textContent.toLowerCase();
        if (text.includes(searchTerm)) {
            item.style.display = '';
        } else {
            item.style.display = 'none';
        }
    });
}

// Save member (create or update)
async function saveMember() {
    const memberId = document.getElementById('memberId').value;
    const memberNumber = document.getElementById('memberNumber').value;
    
    // Member number is now optional
    let memberNumInt = null;
    if (memberNumber && memberNumber.trim() !== '') {
        memberNumInt = parseInt(memberNumber);
        if (memberNumInt <= 0) {
            showMessage('Member number must be a positive number', 'error');
            return;
        }
        
        // Validate member number before saving (only if provided)
        if (!memberId) {
            const isAvailable = await checkMemberNumber(memberNumber);
            if (!isAvailable) {
                showMessage('Please use a different member number.', 'error');
                return;
            }
        }
    }
    
    const seniorityDate = document.getElementById('seniorityDate').value;
    const birthDate = document.getElementById('birthDate').value;

    const member = {
        memberNumber: memberNumInt,
        firstName: document.getElementById('firstName').value,
        lastName: document.getElementById('lastName').value,
        gender: document.getElementById('gender').value,
        role: document.getElementById('role').value,
        photoBase64: document.getElementById('photoUpload').dataset.photoData || null,
        birthDate: birthDate ? birthDate : null,
        email: document.getElementById('email').value || null,
        phoneNumber: document.getElementById('phoneNumber').value || null,
        isAlive: document.getElementById('status').value === 'true',
        seniorityDate: seniorityDate ? seniorityDate : null,
        address: {
            street: document.getElementById('street').value,
            houseNumber: document.getElementById('houseNumber').value || null,
            city: document.getElementById('city').value,
            postalCode: document.getElementById('postalCode').value,
            country: document.getElementById('country').value || null
        },
        customFieldValues: getCustomFieldValues()
    };

    try {
        let response;
        if (memberId) {
            // Update existing member
            member.id = parseInt(memberId);
            if (editingAddressId) {
                member.address.id = editingAddressId;
                member.address.memberId = parseInt(memberId);
            }
            response = await fetchWithAuth(`${API_URL}/${memberId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(member)
            });
        } else {
            // Create new member
            response = await fetchWithAuth(API_URL, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(member)
            });
        }

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.error || 'Failed to save member');
        }

        showMessage('Member saved successfully!', 'success');
        resetForm();
        await loadMembers();
    } catch (error) {
        console.error('Error saving member:', error);
        showMessage(error.message || 'Failed to save member. Please try again.', 'error');
    }
}

// Edit member
async function editMember(id) {
    try {
        const response = await fetch(`${API_URL}/${id}`);
        if (!response.ok) throw new Error('Failed to load member');
        
        const member = await response.json();
        
        document.getElementById('memberId').value = member.id;
        document.getElementById('memberNumber').value = member.memberNumber || '';
        document.getElementById('firstName').value = member.firstName;
        document.getElementById('lastName').value = member.lastName;
        document.getElementById('gender').value = member.gender || 'Man';
        document.getElementById('role').value = member.role || 'Kandidaat';
        document.getElementById('email').value = member.email || '';
        document.getElementById('phoneNumber').value = member.phoneNumber || '';
        document.getElementById('status').value = member.isAlive.toString();

        // Set photo if available
        const photoUploadInput = document.getElementById('photoUpload');
        const photoPreview = document.getElementById('photoPreview');
        const photoPreviewImg = document.getElementById('photoPreviewImg');
        if (member.photoBase64) {
            photoUploadInput.dataset.photoData = member.photoBase64;
            photoPreviewImg.src = member.photoBase64;
            photoPreview.style.display = 'block';
        } else {
            photoUploadInput.dataset.photoData = '';
            photoPreview.style.display = 'none';
        }

        // Set seniority date
        if (member.seniorityDate) {
            const date = new Date(member.seniorityDate);
            document.getElementById('seniorityDate').value = date.toISOString().split('T')[0];
        } else {
            document.getElementById('seniorityDate').value = '';
        }

        // Set birth date
        if (member.birthDate) {
            const date = new Date(member.birthDate);
            document.getElementById('birthDate').value = date.toISOString().split('T')[0];
        } else {
            document.getElementById('birthDate').value = '';
        }
        
        if (member.address) {
            editingAddressId = member.address.id;
            document.getElementById('street').value = member.address.street;
            document.getElementById('houseNumber').value = member.address.houseNumber || '';
            document.getElementById('city').value = member.address.city;
            document.getElementById('postalCode').value = member.address.postalCode;
            document.getElementById('country').value = member.address.country || '';
        }
        
        // Load custom field values
        if (member.customFieldValues && member.customFieldValues.length > 0) {
            setCustomFieldValues(member.customFieldValues);
        } else {
            clearCustomFieldValues();
        }
        
        // Clear member number validation message
        document.getElementById('memberNumberMessage').textContent = '';
        
        document.getElementById('formTitle').textContent = 'Edit Member';
        document.querySelector('.form-section').scrollIntoView({ behavior: 'smooth' });
    } catch (error) {
        console.error('Error loading member:', error);
        showMessage('Failed to load member for editing.', 'error');
    }
}

// Delete member
async function deleteMember(id) {
    if (!confirm('Are you sure you want to delete this member?')) return;

    try {
        const response = await fetchWithAuth(`${API_URL}/${id}`, {
            method: 'DELETE'
        });

        if (!response.ok) throw new Error('Failed to delete member');

        showMessage('Member deleted successfully!', 'success');
        await loadMembers();
    } catch (error) {
        console.error('Error deleting member:', error);
        showMessage('Failed to delete member. Please try again.', 'error');
    }
}

// Export to CSV
async function exportToCSV() {
    try {
        showMessage('Generating CSV export...', 'success');
        
        const response = await fetchWithAuth(`${API_URL}/export/csv`);
        if (!response.ok) throw new Error('Failed to export data');
        
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `members_export_${new Date().toISOString().split('T')[0]}.csv`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
        
        showMessage('CSV export downloaded successfully!', 'success');
    } catch (error) {
        console.error('Error exporting to CSV:', error);
        showMessage('Failed to export CSV. Please try again.', 'error');
    }
}

// Reset form
function resetForm() {
    const form = document.getElementById('memberForm');
    if (form) form.reset();
    
    editingMemberId = null;
    editingAddressId = null;
    document.getElementById('memberId').value = '';
    document.getElementById('addressId').value = '';
    document.getElementById('formTitle').textContent = 'Add New Member';
    document.getElementById('memberNumberMessage').textContent = '';
    
    // Reset photo preview
    const photoPreview = document.getElementById('photoPreview');
    const photoUploadInput = document.getElementById('photoUpload');
    if (photoPreview) photoPreview.style.display = 'none';
    if (photoUploadInput) photoUploadInput.dataset.photoData = '';

    // Clear custom field values
    clearCustomFieldValues();
}

// CSV Import Functions
let csvHeaders = [];
let csvData = [];

function showImportModal() {
    console.log('showImportModal called');
    const modal = document.getElementById('importModal');
    if (!modal) {
        console.error('importModal element not found!');
        alert('Import modal not found. Please refresh the page.');
        return;
    }
    
    console.log('Setting modal display to block');
    modal.style.display = 'block';
    document.body.style.overflow = 'hidden';
    
    const step1 = document.getElementById('importStep1');
    const step2 = document.getElementById('importStep2');
    const step3 = document.getElementById('importStep3');
    const csvFile = document.getElementById('csvFile');
    const csvFileName = document.getElementById('csvFileName');
    
    if (step1) step1.style.display = 'block';
    if (step2) step2.style.display = 'none';
    if (step3) step3.style.display = 'none';
    if (csvFile) csvFile.value = '';
    if (csvFileName) csvFileName.textContent = '';
    
    console.log('Import modal opened successfully');
}

function closeImportModal() {
    document.getElementById('importModal').style.display = 'none';
    document.body.style.overflow = '';
    csvHeaders = [];
    csvData = [];
}

function previewCSV(event) {
    const file = event.target.files[0];
    if (file) {
        document.getElementById('csvFileName').textContent = `Selected: ${file.name} (${(file.size / 1024).toFixed(2)} KB)`;
    }
}

async function processCSVHeaders() {
    const fileInput = document.getElementById('csvFile');
    const file = fileInput.files[0];
    
    if (!file) {
        showMessage('Please select a CSV file', 'error');
        return;
    }
    
    try {
        const text = await file.text();
        const lines = text.split('\n').filter(l => l.trim());
        
        if (lines.length < 2) {
            showMessage('CSV file must have headers and at least one data row', 'error');
            return;
        }
        
        // Detect separator
        const separator = lines[0].includes(';') ? ';' : ',';
        
        // Parse headers
        csvHeaders = parseCsvLine(lines[0], separator);
        csvData = lines.slice(1).map(line => parseCsvLine(line, separator));
        
        // Create field mapping UI
        const container = document.getElementById('fieldMappingContainer');
        const memberFields = [
            { name: 'MemberNumber', label: 'Member Number *', required: true },
            { name: 'FirstName', label: 'First Name *', required: true },
            { name: 'LastName', label: 'Last Name *', required: true },
            { name: 'Gender', label: 'Gender', required: false },
            { name: 'Role', label: 'Role', required: false },
            { name: 'Email', label: 'Email', required: false },
            { name: 'PhoneNumber', label: 'Phone Number', required: false },
            { name: 'BirthDate', label: 'Birth Date', required: false },
            { name: 'SeniorityDate', label: 'Seniority Date', required: false },
            { name: 'IsAlive', label: 'Status (Alive/Deceased)', required: false },
            { name: 'Street', label: 'Street', required: false },
            { name: 'HouseNumber', label: 'House Number', required: false },
            { name: 'City', label: 'City', required: false },
            { name: 'PostalCode', label: 'Postal Code', required: false },
            { name: 'Country', label: 'Country', required: false }
        ];

        // Sort member fields by required status and label
        const sortedMemberFields = memberFields.sort((a, b) => {
            if (a.required && !b.required) return -1;
            if (!a.required && b.required) return 1;
            return a.label.localeCompare(b.label);
        });
        
        container.innerHTML = sortedMemberFields.map(field => {
            const options = ['<option value="">-- Do not import --</option>']
                .concat(csvHeaders.map((h, i) => 
                    `<option value="${escapeHtml(h)}" ${h.toLowerCase().includes(field.name.toLowerCase()) ? 'selected' : ''}>${escapeHtml(h)}</option>`
                )).join('');
            
            return `
                <div class="form-group">
                    <label for="map_${field.name}">${field.label}</label>
                    <select id="map_${field.name}" class="field-mapping-select">
                        ${options}
                    </select>
                </div>
            `;
        }).join('');
        
        // Show step 2
        document.getElementById('importStep1').style.display = 'none';
        document.getElementById('importStep2').style.display = 'block';
        
    } catch (error) {
        console.error('Error processing CSV:', error);
        showMessage('Failed to process CSV file', 'error');
    }
}

function parseCsvLine(line, separator = ',') {
    const values = [];
    let current = '';
    let inQuotes = false;
    
    for (let i = 0; i < line.length; i++) {
        const char = line[i];
        
        if (char === '"') {
            inQuotes = !inQuotes;
        } else if (char === separator && !inQuotes) {
            values.push(current.trim());
            current = '';
        } else {
            current += char;
        }
    }
    
    values.push(current.trim());
    return values.map(v => v.replace(/^"|"$/g, ''));
}

function backToStep1() {
    document.getElementById('importStep2').style.display = 'none';
    document.getElementById('importStep1').style.display = 'block';
}

async function importCSVData() {
    try {
        // Get field mapping
        const mapping = {};
        const memberFields = ['MemberNumber', 'FirstName', 'LastName', 'Gender', 'Role', 'Email', 
                             'PhoneNumber', 'BirthDate', 'SeniorityDate', 'IsAlive', 'Street', 
                             'HouseNumber', 'City', 'PostalCode', 'Country'];
        
        memberFields.forEach(field => {
            const select = document.getElementById(`map_${field}`);
            if (select && select.value) {
                mapping[field] = select.value;
            }
        });
        
        // Validate required fields are mapped
        if (!mapping.MemberNumber || !mapping.FirstName || !mapping.LastName) {
            showMessage('Please map required fields: Member Number, First Name, and Last Name', 'error');
            return;
        }
        
        // Prepare form data
        const fileInput = document.getElementById('csvFile');
        const formData = new FormData();
        formData.append('csvFile', fileInput.files[0]);
        formData.append('fieldMapping', JSON.stringify(mapping));
        
        // Show loading message
        showMessage('Importing members...', 'success');
        
        // Send to server
        const response = await fetchWithAuth(`${API_URL}/import/csv`, {
            method: 'POST',
            body: formData
        });
        
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Import failed');
        }
        
        const result = await response.json();
        
        // Show results
        document.getElementById('importStep2').style.display = 'none';
        document.getElementById('importStep3').style.display = 'block';
        
        let resultsHTML = `
            <div style="text-align: center; padding: 20px;">
                <div style="font-size: 48px; margin-bottom: 20px;">?</div>
                <h3 style="color: #4caf50; margin-bottom: 20px;">Import Completed!</h3>
                <div style="background: #f5f5f5; padding: 20px; border-radius: 8px; margin-bottom: 20px;">
                    <div style="font-size: 24px; font-weight: bold; color: #4caf50; margin-bottom: 10px;">
                        ${result.importedCount}
                    </div>
                    <div style="color: #666;">Members imported successfully</div>
                </div>
        `;
        
        if (result.skippedCount > 0) {
            resultsHTML += `
                <div style="background: #fff3cd; padding: 15px; border-radius: 8px; margin-bottom: 20px; border-left: 4px solid #ffc107;">
                    <div style="font-weight: bold; color: #856404; margin-bottom: 5px;">
                        ${result.skippedCount} members skipped
                    </div>
                    <div style="color: #856404; font-size: 14px;">
                        These members already exist (duplicate member numbers)
                    </div>
                </div>
            `;
        }
        
        if (result.errors && result.errors.length > 0) {
            resultsHTML += `
                <div style="background: #ffebee; padding: 15px; border-radius: 8px; text-align: left; border-left: 4px solid #d32f2f;">
                    <div style="font-weight: bold; color: #c62828; margin-bottom: 10px;">
                        ${result.errorCount} errors occurred:
                    </div>
                    <ul style="color: #c62828; font-size: 13px; margin: 0; padding-left: 20px;">
                        ${result.errors.map(err => `<li>${escapeHtml(err)}</li>`).join('')}
                    </ul>
                    ${result.errorCount > 10 ? `<div style="color: #c62828; font-size: 12px; margin-top: 10px;">And ${result.errorCount - 10} more...</div>` : ''}
                </div>
            `;
        }
        
        resultsHTML += '</div>';
        
        document.getElementById('importResults').innerHTML = resultsHTML;
        showMessage(`Import completed! ${result.importedCount} members imported.`, 'success');
        
    } catch (error) {
        console.error('Import error:', error);
        showMessage(error.message || 'Failed to import CSV', 'error');
    }
}

// Photo upload handler
function handlePhotoUpload(event) {
    const file = event.target.files[0];
    if (!file) return;
    
    // Check file size (max 5MB)
    if (file.size > 5 * 1024 * 1024) {
        showMessage('Photo size must be less than 5MB', 'error');
        event.target.value = '';
        return;
    }
    
    // Check file type
    if (!file.type.startsWith('image/')) {
        showMessage('Please select a valid image file', 'error');
        event.target.value = '';
        return;
    }
    
    const reader = new FileReader();
    reader.onload = function(e) {
        const photoData = e.target.result;
        document.getElementById('photoUpload').dataset.photoData = photoData;
        document.getElementById('photoPreviewImg').src = photoData;
        document.getElementById('photoPreview').style.display = 'block';
    };
    reader.readAsDataURL(file);
}

// Message display helper
function showMessage(message, type = 'success') {
    // Remove existing message if any
    const existingMessage = document.querySelector('.message-toast');
    if (existingMessage) {
        existingMessage.remove();
    }
    
    const messageDiv = document.createElement('div');
    messageDiv.className = `message-toast message-${type}`;
    messageDiv.textContent = message;
    messageDiv.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        padding: 15px 25px;
        background: ${type === 'success' ? '#4caf50' : '#d32f2f'};
        color: white;
        border-radius: 4px;
        box-shadow: 0 2px 8px rgba(0,0,0,0.2);
        z-index: 10000;
        animation: slideIn 0.3s ease-out;
    `;
    
    document.body.appendChild(messageDiv);
    
    setTimeout(() => {
        messageDiv.style.animation = 'slideOut 0.3s ease-out';
        setTimeout(() => messageDiv.remove(), 300);
    }, 3000);
}

// Custom Fields Functions
let customFieldsCache = [];

async function loadCustomFieldsForForm() {
    try {
        const response = await fetchWithAuth('/api/settings/custom-fields');
        if (!response.ok) return;
        
        const fields = await response.json();
        customFieldsCache = fields.filter(f => f.isActive);
        
        if (customFieldsCache.length > 0) {
            renderCustomFieldsInForm();
        }
    } catch (error) {
        console.error('Error loading custom fields:', error);
    }
}

function renderCustomFieldsInForm() {
    const container = document.getElementById('customFieldsContainer');
    const section = document.getElementById('customFieldsSection');
    
    if (!container || customFieldsCache.length === 0) return;
    
    section.style.display = 'block';
    
    container.innerHTML = customFieldsCache
        .sort((a, b) => a.displayOrder - b.displayOrder)
        .map(field => {
            const required = field.isRequired ? 'required' : '';
            const requiredLabel = field.isRequired ? ' *' : '';
            
            let inputHTML = '';
            
            switch (field.fieldType) {
                case 'Text':
                    inputHTML = `<input type="text" id="cf_${field.id}" ${required} placeholder="Enter ${field.fieldLabel}">`;
                    break;
                case 'Number':
                    inputHTML = `<input type="number" id="cf_${field.id}" ${required} placeholder="Enter ${field.fieldLabel}">`;
                    break;
                case 'Date':
                    inputHTML = `<input type="date" id="cf_${field.id}" ${required}>`;
                    break;
                case 'Checkbox':
                    inputHTML = `

                        <div style="display: flex; align-items: center; gap: 10px;">
                            <input type="checkbox" id="cf_${field.id}" style="width: auto; cursor: pointer;">
                            <label for="cf_${field.id}" style="margin: 0; cursor: pointer;">${escapeHtml(field.fieldLabel)}</label>
                        </div>
                    `;
                    break;
                default:
                    inputHTML = `<input type="text" id="cf_${field.id}" ${required}>`;
            }
            
            if (field.fieldType === 'Checkbox') {
                return `<div class="form-group">${inputHTML}</div>`;
            }
            
            return `
                <div class="form-group">
                    <label for="cf_${field.id}">${escapeHtml(field.fieldLabel)}${requiredLabel}</label>
                    ${inputHTML}
                </div>
            `;
        }).join('');
}

function getCustomFieldValues() {
    const values = [];
    
    customFieldsCache.forEach(field => {
        const input = document.getElementById(`cf_${field.id}`);
        if (!input) return;
        
        let value = '';
        
        if (field.fieldType === 'Checkbox') {
            value = input.checked ? 'true' : 'false';
        } else {
            value = input.value || '';
        }
        
        if (value || field.isRequired) {
            values.push({
                customFieldId: field.id,
                value: value
            });
        }
    });
    
    return values;
}

function setCustomFieldValues(customFieldValues) {
    if (!customFieldValues || !Array.isArray(customFieldValues)) return;
    
    customFieldValues.forEach(cfv => {
        const input = document.getElementById(`cf_${cfv.customFieldId}`);
        if (!input) return;
        
        if (cfv.customField?.fieldType === 'Checkbox') {
            input.checked = cfv.value === 'true' || cfv.value === '1';
        } else if (cfv.customField?.fieldType === 'Date' && cfv.value) {
            const date = new Date(cfv.value);
            input.value = date.toISOString().split('T')[0];
        } else {
            input.value = cfv.value || '';
        }
    });
}

function clearCustomFieldValues() {
    customFieldsCache.forEach(field => {
        const input = document.getElementById(`cf_${field.id}`);
        if (!input) return;
        
        if (field.fieldType === 'Checkbox') {
            input.checked = false;
        } else {
            input.value = '';
        }
    });
}

// Remove photo
function removePhoto() {
    console.log('removePhoto called');
    
    const photoUploadInput = document.getElementById('photoUpload');
    const photoPreview = document.getElementById('photoPreview');
    const photoPreviewImg = document.getElementById('photoPreviewImg');
    
    if (!photoUploadInput || !photoPreview) {
        console.error('Photo elements not found');
        alert('Error: Photo elements not found. Please refresh the page.');
        return;
    }
    
    console.log('Removing photo...');
    
    // Clear the file input
    photoUploadInput.value = '';
    
    // Clear the base64 data
    if (photoUploadInput.dataset) {
        photoUploadInput.dataset.photoData = '';
    }
    
    // Hide the preview
    photoPreview.style.display = 'none';
    
    // Clear the preview image
    if (photoPreviewImg) {
        photoPreviewImg.src = '';
    }
    
    console.log('Photo removed successfully');
}

// Backup and Restore Functions
function showBackupModal() {
    document.getElementById('backupModal').style.display = 'block';
    document.body.style.overflow = 'hidden';
}

function closeBackupModal() {
    document.getElementById('backupModal').style.display = 'none';
    document.body.style.overflow = '';
}

function showRestoreModal() {
    document.getElementById('restoreModal').style.display = 'block';
    document.body.style.overflow = 'hidden';
}

function closeRestoreModal() {
    document.getElementById('restoreModal').style.display = 'none';
    document.body.style.overflow = '';
}

async function createBackup() {
    try {
        const password = document.getElementById('backupPassword')?.value || '';
        showMessage('Creating backup...', 'success');
        
        const url = password ? `${API_URL}/backup?password=${encodeURIComponent(password)}` : `${API_URL}/backup`;
        const response = await fetchWithAuth(url, { method: 'POST' });
        
        if (!response.ok) throw new Error('Failed to create backup');
        
        const blob = await response.blob();
        const downloadUrl = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = downloadUrl;
        a.download = `members_backup_${new Date().toISOString().split('T')[0]}.bak`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(downloadUrl);
        document.body.removeChild(a);
        
        showMessage('Backup created and downloaded successfully!', 'success');
        closeBackupModal();
    } catch (error) {
        console.error('Error creating backup:', error);
        showMessage('Failed to create backup. Please try again.', 'error');
    }
}

async function restoreBackup() {
    try {
        const fileInput = document.getElementById('restoreFile');
        const file = fileInput?.files[0];
        
        if (!file) {
            showMessage('Please select a backup file', 'error');
            return;
        }
        
        const password = document.getElementById('restorePassword')?.value || '';
        const overwrite = document.getElementById('overwriteData')?.checked || false;
        
        showMessage('Restoring backup...', 'success');
        
        const formData = new FormData();
        formData.append('backupFile', file);
        
        let url = `${API_URL}/restore?overwrite=${overwrite}`;
        if (password) {
            url += `&password=${encodeURIComponent(password)}`;
        }
        
        const response = await fetchWithAuth(url, {
            method: 'POST',
            body: formData
        });
        
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Failed to restore backup');
        }
        
        const result = await response.json();
        showMessage(`Backup restored! ${result.importedMembers} members imported, ${result.skippedMembers} skipped.`, 'success');
        closeRestoreModal();
        await loadMembers();
    } catch (error) {
        console.error('Error restoring backup:', error);
        showMessage(error.message || 'Failed to restore backup.', 'error');
    }
}

// Delete All Members (Test Function)
async function deleteAllMembers() {
    if (!confirm('?? WARNING: This will DELETE ALL MEMBERS!\n\nThis action cannot be undone.\n\nAre you absolutely sure?')) {
        return;
    }
    
    if (!confirm('This is your last chance!\n\nClick OK to permanently delete all members.')) {
        return;
    }
    
    try {
        showMessage('Deleting all members...', 'success');
        
        const response = await fetchWithAuth(`${API_URL}/delete-all`, {
            method: 'DELETE'
        });
        
        if (!response.ok) throw new Error('Failed to delete members');
        
        const result = await response.json();
        showMessage(`Successfully deleted ${result.deletedCount} members!`, 'success');
        await loadMembers();
    } catch (error) {
        console.error('Error deleting members:', error);
        showMessage('Failed to delete members. Please try again.', 'error');
    }
}

// Bulk Update Functions
function showBulkUpdateModal() {
    const modal = document.getElementById('bulkUpdateModal');
    const memberList = document.getElementById('bulkMemberList');
    
    if (!currentMembers || currentMembers.length === 0) {
        showMessage('No members available for bulk update', 'error');
        return;
    }
    
    // Populate member list with checkboxes
    memberList.innerHTML = currentMembers.map(member => `
        <div style="padding: 8px; border-bottom: 1px solid #eee;">
            <label style="display: flex; align-items: center; cursor: pointer; gap: 10px;">
                <input type="checkbox" class="bulk-member-checkbox" value="${member.id}" style="cursor: pointer;">
                <span>${escapeHtml(member.memberNumber)} - ${escapeHtml(member.firstName)} ${escapeHtml(member.lastName)}</span>
            </label>
        </div>
    `).join('');
    
    modal.style.display = 'block';
    document.body.style.overflow = 'hidden';
}

function closeBulkUpdateModal() {
    document.getElementById('bulkUpdateModal').style.display = 'none';
    document.body.style.overflow = '';
    
    // Reset form
    document.getElementById('bulkGender').value = '';
    document.getElementById('bulkRole').value = '';
    document.getElementById('bulkStatus').value = '';
}

async function applyBulkUpdate() {
    try {
        // Get selected member IDs
        const checkboxes = document.querySelectorAll('.bulk-member-checkbox:checked');
        const memberIds = Array.from(checkboxes).map(cb => parseInt(cb.value));
        
        if (memberIds.length === 0) {
            showMessage('Please select at least one member', 'error');
            return;
        }
        
        // Get update values
        const gender = document.getElementById('bulkGender')?.value || '';
        const role = document.getElementById('bulkRole')?.value || '';
        const status = document.getElementById('bulkStatus')?.value || '';
        
        if (!gender && !role && !status) {
            showMessage('Please select at least one field to update', 'error');
            return;
        }
        
        const updateData = {
            memberIds: memberIds,
            gender: gender || null,
            role: role || null,
            isAlive: status ? status === 'true' : null
        };
        
        showMessage('Updating members...', 'success');
        
        const response = await fetchWithAuth(`${API_URL}/bulk-update`, {
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(updateData)
        });
        
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Failed to update members');
        }
        
        const result = await response.json();
        showMessage(`Successfully updated ${result.updatedCount} members!`, 'success');
        closeBulkUpdateModal();
        await loadMembers();
    } catch (error) {
        console.error('Error in bulk update:', error);
        showMessage(error.message || 'Failed to update members.', 'error');
    }
}

// Explicitly make critical functions globally accessible for onclick handlers
window.showImportModal = showImportModal;
window.closeImportModal = closeImportModal;
window.showBackupModal = showBackupModal;
window.closeBackupModal = closeBackupModal;
window.showRestoreModal = showRestoreModal;
window.closeRestoreModal = closeRestoreModal;
window.deleteAllMembers = deleteAllMembers;
window.exportToCSV = exportToCSV;
window.showBulkUpdateModal = showBulkUpdateModal;
window.closeBulkUpdateModal = closeBulkUpdateModal;
window.sortMembers = sortMembers;
window.filterMembers = filterMembers;
window.resetForm = resetForm;
window.showContactCard = showContactCard;
window.closeContactModal = closeContactModal;
window.editMember = editMember;
window.editMemberFromCard = editMemberFromCard;
window.deleteMember = deleteMember;
window.handlePhotoUpload = handlePhotoUpload;
window.removePhoto = removePhoto;
window.previewCSV = previewCSV;
window.processCSVHeaders = processCSVHeaders;
window.backToStep1 = backToStep1;
window.importCSVData = importCSVData;
window.createBackup = createBackup;
window.restoreBackup = restoreBackup;
window.applyBulkUpdate = applyBulkUpdate;

console.log('? All functions loaded and globally accessible');
console.log('API_URL:', API_URL);
